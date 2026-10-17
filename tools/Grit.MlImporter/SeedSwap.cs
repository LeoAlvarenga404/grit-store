namespace Grit.MlImporter;

// Each step is undone in reverse order when a later one throws, so a failed swap never leaves the seed half replaced.
internal static class SeedSwap
{
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

    private static void Move(string source, string destination)
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
