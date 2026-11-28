using Roomy.Core;

namespace Roomy.Tests;

/// <summary>
/// Every step honors the cancellation token before it starts on an item, and before the one
/// uninterruptible Windows call (emptying the Recycle Bin). A run that is already cancelled
/// must throw before touching anything — checked here with plain temp files, never shell paths.
/// </summary>
public sealed class CleanerTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "RoomyTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void ACancelledRunThrowsBeforeTheFirstDelete()
    {
        var dir = Path.Combine(_root, "cache");
        Directory.CreateDirectory(dir);
        var one = Path.Combine(dir, "one.bin");
        var two = Path.Combine(dir, "two.bin");
        File.WriteAllBytes(one, new byte[1024]);
        File.WriteAllBytes(two, new byte[2048]);

        var category = new CleanupCategory { Id = "apps", Title = "Caches", Description = "", Glyph = "" };
        category.Items.Add(new CleanupItem { Name = "one", Paths = [one], Size = 1024 });
        category.Items.Add(new CleanupItem { Name = "two", Paths = [two], Size = 2048 });

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.Throws<OperationCanceledException>(() => Cleaner.Run([new CleanJob(category, category.Items)], null, ct: cts.Token));
        Assert.True(File.Exists(one), "the first item must not be deleted by a cancelled run");
        Assert.True(File.Exists(two), "the second item must not be deleted by a cancelled run");
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }
}
