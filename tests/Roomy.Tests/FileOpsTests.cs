using Roomy.Core;

namespace Roomy.Tests;

/// <summary>
/// minAge keeps files that are still in use (recently written) out of the measurement,
/// and the same cutoff guards the delete later. These run against the real handle-based
/// checks on Windows; off Windows Measure safely refuses, so they only assert there.
/// </summary>
public sealed class FileOpsTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "RoomyTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public void MeasureCountsOnlyFilesOlderThanTheMinimumAge()
    {
        if (!OperatingSystem.IsWindows()) return;

        var dir = Path.Combine(_root, "ages");
        Directory.CreateDirectory(dir);
        var old = Path.Combine(dir, "old.bin");
        var fresh = Path.Combine(dir, "fresh.bin");
        File.WriteAllBytes(old, new byte[1024]);
        File.WriteAllBytes(fresh, new byte[2048]);
        File.SetLastWriteTimeUtc(old, DateTime.UtcNow - TimeSpan.FromDays(2));

        var (bytes, files) = FileOps.Measure(dir, TimeSpan.FromDays(1));
        Assert.Equal(1024, bytes);
        Assert.Equal(1, files);

        var (allBytes, allFiles) = FileOps.Measure(dir, null);
        Assert.Equal(3072, allBytes);
        Assert.Equal(2, allFiles);
    }

    [Fact]
    public void MeasureChecksTheAgeOfASingleFileToo()
    {
        if (!OperatingSystem.IsWindows()) return;

        var oldFile = Path.Combine(_root, "old.bin");
        var freshFile = Path.Combine(_root, "fresh.bin");
        Directory.CreateDirectory(_root);
        File.WriteAllBytes(oldFile, new byte[1024]);
        File.WriteAllBytes(freshFile, new byte[2048]);
        File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow - TimeSpan.FromDays(2));

        var (oldBytes, oldFiles) = FileOps.Measure(oldFile, TimeSpan.FromDays(1));
        Assert.Equal(1024, oldBytes);
        Assert.Equal(1, oldFiles);
        var (freshBytes, freshFiles) = FileOps.Measure(freshFile, TimeSpan.FromDays(1));
        Assert.Equal(0, freshBytes);
        Assert.Equal(0, freshFiles);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }
}
