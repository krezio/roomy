using System.Diagnostics;
using Roomy.Core;

namespace Roomy.Tests;

/// <summary>
/// The space map walks a whole drive. Pin the maths (sizes, own-file counts, the biggest-files cap)
/// and the guard rails (reparse points are never followed, cancellation is honored).
/// </summary>
public sealed class SpaceScannerTests : IDisposable
{
    readonly string _root = Path.Combine(Path.GetTempPath(), "RoomyTests", Guid.NewGuid().ToString("N"));
    const long MB = 1024L * 1024;

    static void FolderWithFile(string dir, long bytes)
    {
        Directory.CreateDirectory(dir);
        using var f = File.Create(Path.Combine(dir, "data.bin"));
        f.SetLength(bytes);
    }

    /// <summary>A junction on Windows, a symlink elsewhere. Both are reparse points to the scanner.</summary>
    static void LinkFolder(string link, string target)
    {
        if (OperatingSystem.IsWindows())
        {
            var psi = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
            {
                CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            };
            using var p = Process.Start(psi)!;
            p.WaitForExit();
        }
        else
        {
            Directory.CreateSymbolicLink(link, target);
        }
        Assert.True(Directory.Exists(link), "the link could not be created here");
    }

    [Fact]
    public void SizesAddUpAcrossNestedAndParallelBranches()
    {
        FolderWithFile(_root, 512 * 1024);                    // a file directly in the scanned root
        FolderWithFile(Path.Combine(_root, "a"), 1 * MB);
        FolderWithFile(Path.Combine(_root, "b", "c"), 2 * MB);
        FolderWithFile(Path.Combine(_root, "b", "d"), 3 * MB);
        FolderWithFile(Path.Combine(_root, "e"), 4 * MB);

        var scan = new SpaceScanner().Scan(_root);

        Assert.Equal(10 * MB + 512 * 1024, scan.Size);
        Assert.Equal(5 * MB, scan.Children.First(x => x.Name == "b").Size);
        // FileCount and FileBytes cover the folder's own files, not the subtree.
        Assert.Equal(1, scan.FileCount);
        Assert.Equal(512 * 1024, scan.FileBytes);
        Assert.Equal(1, scan.Children.First(x => x.Name == "a").FileCount);
        // Biggest branch first, the order the map shows.
        Assert.Equal(new[] { "b", "e", "a" }, scan.Children.Select(x => x.Name).ToArray());
    }

    [Fact]
    public void ReparsePointsAreNeverFollowed()
    {
        FolderWithFile(Path.Combine(_root, "scan", "real"), 1 * MB);
        FolderWithFile(Path.Combine(_root, "outside", "big"), 2 * MB);
        LinkFolder(Path.Combine(_root, "scan", "linked"), Path.Combine(_root, "outside", "big"));

        var scan = new SpaceScanner().Scan(Path.Combine(_root, "scan"));

        Assert.Equal(1 * MB, scan.Size);
        Assert.DoesNotContain(scan.Children, x => x.Name == "linked");
    }

    [Fact]
    public void OnlyTheBiggestFortyFilesSurviveAndTheFloorRises()
    {
        var root = Path.Combine(_root, "big");
        Directory.CreateDirectory(root);
        for (int i = 0; i < 85; i++)
        {
            using var f = File.Create(Path.Combine(root, $"f{i:00}.bin"));
            f.SetLength((100 + i) * MB);                      // 100..184 MB, all above the 50 MB floor
        }

        var scanner = new SpaceScanner();
        scanner.Scan(root);

        var big = scanner.BigFiles;
        Assert.Equal(40, big.Count);
        Assert.Equal(big.OrderByDescending(b => b.Size), big);
        // Everything below the 40th biggest (145 MB) was dropped.
        Assert.All(big, b => Assert.True(b.Size >= 145 * MB, $"{b.FullPath} is {b.Size} bytes"));

        // The floor rose with them: a later scan on the same scanner no longer tracks a 60 MB file.
        using (var f = File.Create(Path.Combine(root, "medium60.bin"))) f.SetLength(60 * MB);
        scanner.Scan(root);
        Assert.DoesNotContain(scanner.BigFiles, b => b.FullPath.EndsWith("medium60.bin"));
    }

    [Fact]
    public void CancelledTokenStopsTheScan()
    {
        FolderWithFile(Path.Combine(_root, "data"), 1 * MB);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => new SpaceScanner().Scan(_root, cts.Token));
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }
}
