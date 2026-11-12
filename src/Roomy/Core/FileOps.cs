using System.IO.Enumeration;

namespace Roomy.Core;

/// <summary>Measuring and deleting, with guard rails.</summary>
public static class FileOps
{
    public readonly record struct DeleteResult(long Freed, int Files, int Failed);

    static EnumerationOptions Deep() => new()
    {
        RecurseSubdirectories = true,
        IgnoreInaccessible = true,
        // Never follow junctions/symlinks: they can point anywhere.
        AttributesToSkip = FileAttributes.ReparsePoint,
        ReturnSpecialDirectories = false,
    };

    static DateTime Cutoff(TimeSpan? minAge) => minAge is { } a ? DateTime.UtcNow - a : DateTime.MaxValue;

    public static bool Exists(string p) => File.Exists(p) || Directory.Exists(p);

    public static (long Bytes, int Files) Measure(string path, TimeSpan? minAge, CancellationToken ct = default)
    {
        long bytes = 0;
        int files = 0;
        var cutoff = Cutoff(minAge);
        try
        {
            if (File.Exists(path))
            {
                var fi = new FileInfo(path);
                return fi.LastWriteTimeUtc <= cutoff ? (fi.Length, 1) : (0, 0);
            }
            if (!Directory.Exists(path) || IsReparse(path)) return (0, 0);

            var e = new FileSystemEnumerable<(long Len, DateTime Mod)>(path,
                (ref FileSystemEntry x) => (x.Length, x.LastWriteTimeUtc.UtcDateTime), Deep())
            {
                ShouldIncludePredicate = (ref FileSystemEntry x) => !x.IsDirectory,
            };
            foreach (var (len, mod) in e)
            {
                ct.ThrowIfCancellationRequested();
                if (mod <= cutoff) { bytes += len; files++; }
            }
        }
        catch (OperationCanceledException) { throw; }
        catch { /* keep whatever we counted */ }
        return (bytes, files);
    }

    /// <summary>
    /// Deletes a file, or the contents of a folder (the folder itself stays).
    /// Files that are in use are skipped, never forced.
    /// </summary>
    public static DeleteResult Delete(string path, TimeSpan? minAge, CancellationToken ct = default)
    {
        var cutoff = Cutoff(minAge);

        if (File.Exists(path))
        {
            Guard(path);
            var fi = new FileInfo(path);
            if (fi.LastWriteTimeUtc > cutoff) return default;
            long len = fi.Length;
            return TryDeleteFile(path) ? new(len, 1, 0) : new(0, 0, 1);
        }

        if (!Directory.Exists(path) || IsReparse(path)) return default;
        Guard(path);

        List<(string, long, DateTime)> files;
        try
        {
            files = new FileSystemEnumerable<(string, long, DateTime)>(path,
                (ref FileSystemEntry x) => (x.ToFullPath(), x.Length, x.LastWriteTimeUtc.UtcDateTime), Deep())
            {
                ShouldIncludePredicate = (ref FileSystemEntry x) => !x.IsDirectory,
            }.ToList();
        }
        catch { return default; }

        long freed = 0;
        int ok = 0, failed = 0;
        foreach (var (p, len, mod) in files)
        {
            ct.ThrowIfCancellationRequested();
            if (mod > cutoff) continue;
            if (TryDeleteFile(p)) { freed += len; ok++; }
            else failed++;
        }

        // Tidy up now-empty subfolders, deepest first. Never removes the root.
        try
        {
            var dirs = new FileSystemEnumerable<string>(path, (ref FileSystemEntry x) => x.ToFullPath(), Deep())
            {
                ShouldIncludePredicate = (ref FileSystemEntry x) => x.IsDirectory,
            }.ToList();
            foreach (var d in dirs.OrderByDescending(d => d.Count(ch => ch == '\\')))
            {
                try { Directory.Delete(d, recursive: false); } catch { }
            }
        }
        catch { }

        return new(freed, ok, failed);
    }

    static bool TryDeleteFile(string p)
    {
        try
        {
            var a = File.GetAttributes(p);
            if ((a & FileAttributes.ReadOnly) != 0) File.SetAttributes(p, a & ~FileAttributes.ReadOnly);
            File.Delete(p);
            return true;
        }
        catch { return false; }
    }

    static bool IsReparse(string dir)
    {
        try { return (File.GetAttributes(dir) & FileAttributes.ReparsePoint) != 0; }
        catch { return true; }
    }

    static readonly string[] Protected = new[]
    {
        Env.SystemRoot, Env.Windows, Path.Combine(Env.Windows, "System32"), Env.UserProfile,
        Path.GetDirectoryName(Env.UserProfile) ?? "", Env.ProgramFiles, Env.ProgramFilesX86,
        Env.ProgramData, Env.Local, Env.Roaming, Env.Downloads,
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
        Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
        Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
    }.Where(s => s.Length > 0).Select(Norm).ToArray();

    static string Norm(string p)
    {
        try { return Path.GetFullPath(p).TrimEnd('\\'); }
        catch { return p.TrimEnd('\\'); }
    }

    /// <summary>Last line of defence: never wipe a drive root or a top-level personal/system folder.</summary>
    static void Guard(string path)
    {
        var n = Norm(path);
        if (n.Length <= 3 || Protected.Any(p => string.Equals(p, n, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Refusing to clean a protected location: {path}");
    }
}
