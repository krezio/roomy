namespace Roomy.Core;

public sealed record CleanJob(CleanupCategory Category, IReadOnlyList<CleanupItem> Items);

public sealed record CleanResult(long Freed, long Skipped, int Files);

public static class Cleaner
{
    public static CleanResult Run(IReadOnlyList<CleanJob> jobs, IProgress<(double Fraction, string Step)>? progress, CancellationToken ct = default)
    {
        long total = Math.Max(1, jobs.Sum(j => j.Items.Sum(i => i.Size)));
        long done = 0, freed = 0, skipped = 0;
        int files = 0;

        foreach (var job in jobs)
        {
            var step = $"Cleaning {job.Category.Title.ToLowerInvariant()}…";
            progress?.Report(((double)done / total, step));

            if (job.Category.Action == CleanAction.EmptyRecycleBin)
            {
                long size = job.Items.Sum(i => i.Size);
                var (before, _) = Native.QueryRecycleBin();
                Native.SHEmptyRecycleBin(IntPtr.Zero, null,
                    Native.SHERB_NOCONFIRMATION | Native.SHERB_NOPROGRESSUI | Native.SHERB_NOSOUND);
                var (after, _) = Native.QueryRecycleBin();
                long got = Math.Max(0, before - after);
                freed += got;
                skipped += Math.Max(0, size - got);
                done += size;
                continue;
            }

            if (job.Category.Action != CleanAction.Delete) continue;

            foreach (var item in job.Items)
            {
                ct.ThrowIfCancellationRequested();
                long got = 0;
                foreach (var p in item.Paths)
                {
                    try
                    {
                        var r = FileOps.Delete(p, item.MinAge, ct);
                        got += r.Freed;
                        files += r.Files;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch { /* protected or vanished: skip */ }
                }
                freed += got;
                skipped += Math.Max(0, item.Size - got);
                done += item.Size;
                progress?.Report(((double)done / total, step));
            }
        }

        progress?.Report((1, "All done"));
        return new CleanResult(freed, skipped, files);
    }
}
