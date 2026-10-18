namespace Grit.MlImporter;

// Each step is undone in reverse order when a later one throws, so a failed swap never leaves the seed half replaced.
internal static class SeedSwap
{
    // ponytail: Windows antivirus and the indexer hold just-written files for a moment; 5 tries 500 ms apart is the knob
    // to raise if a real run still loses its swap.
    private const int MoveAttempts = 5;

    private static readonly TimeSpan MoveRetryDelay = TimeSpan.FromMilliseconds(500);

    private static readonly string[] Entries = ["images", "catalog.json"];

    public static void Replace(string outputDirectory, string stagingDirectory)
    {
        var backup = Path.Combine(outputDirectory, $".backup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(backup);
        var undo = new Stack<Action>();
        try
        {
            MoveAside(outputDirectory, backup, undo);
            MoveIn(stagingDirectory, outputDirectory, undo);
        }
        catch
        {
            Restore(undo, backup);
            throw;
        }

        Directory.Delete(backup, recursive: true);
    }

    // The previous entries leave first, so a staged one never overwrites them.
    private static void MoveAside(string outputDirectory, string backup, Stack<Action> undo)
    {
        foreach (var entry in Entries)
        {
            var current = Path.Combine(outputDirectory, entry);
            if (!Exists(current))
            {
                continue;
            }

            var previous = Path.Combine(backup, entry);
            Move(current, previous);
            undo.Push(() => Move(previous, current));
        }
    }

    private static void MoveIn(string stagingDirectory, string outputDirectory, Stack<Action> undo)
    {
        foreach (var entry in Entries)
        {
            var current = Path.Combine(outputDirectory, entry);
            Move(Path.Combine(stagingDirectory, entry), current);
            undo.Push(() => Delete(current));
        }
    }

    // The backup folder is removed only once every undo step ran; if one fails, the old files stay in it.
    private static void Restore(Stack<Action> undo, string backup)
    {
        while (undo.Count > 0)
        {
            undo.Pop()();
        }

        Directory.Delete(backup, recursive: true);
    }

    private static bool Exists(string path) => Directory.Exists(path) || File.Exists(path);

    private static void Move(string source, string destination) => Move(source, destination, MoveOnce, Thread.Sleep);

    // The seams let a test count attempts and waits without a real locked file.
    internal static void Move(string source, string destination, Action<string, string> moveOnce, Action<TimeSpan> wait)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                moveOnce(source, destination);
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException && attempt < MoveAttempts)
            {
                wait(MoveRetryDelay);
            }
        }
    }

    private static void MoveOnce(string source, string destination)
    {
        if (Directory.Exists(source))
        {
            Directory.Move(source, destination);
            return;
        }

        File.Move(source, destination);
    }

    private static void Delete(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
            return;
        }

        File.Delete(path);
    }
}
