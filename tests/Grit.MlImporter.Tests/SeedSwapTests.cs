namespace Grit.MlImporter.Tests;

// Windows antivirus and the indexer hold a just-written file for a moment; the swap waits for it instead of losing the
// run. On Linux an open file can be renamed, so there the test passes without exercising the retry.
public sealed class SeedSwapTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"grit-swap-{Guid.NewGuid():N}");

    [Fact]
    public async Task Replace_StagedFileHeldForAMoment_SwapsOnceItIsReleased()
    {
        var output = Directory.CreateDirectory(Path.Combine(_root, "seed")).FullName;
        var staging = Directory.CreateDirectory(Path.Combine(_root, "staging")).FullName;
        Directory.CreateDirectory(Path.Combine(staging, "images"));
        await File.WriteAllTextAsync(Path.Combine(output, "catalog.json"), "old", TestContext.Current.CancellationToken);
        var staged = Path.Combine(staging, "catalog.json");
        await File.WriteAllTextAsync(staged, "new", TestContext.Current.CancellationToken);
        using var holder = new FileStream(staged, FileMode.Open, FileAccess.Read, FileShare.None);
        var release = Task.Run(async () =>
        {
            await Task.Delay(700);
            holder.Dispose();
        }, TestContext.Current.CancellationToken);

        SeedSwap.Replace(output, staging);
        await release;

        (await File.ReadAllTextAsync(Path.Combine(output, "catalog.json"), TestContext.Current.CancellationToken)).ShouldBe("new");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }
}
