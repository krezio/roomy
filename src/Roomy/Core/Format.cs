namespace Roomy.Core;

public static class Format
{
    static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static string Bytes(long bytes)
    {
        if (bytes <= 0) return "0 B";
        double v = bytes;
        int i = 0;
        while (v >= 1024 && i < Units.Length - 1) { v /= 1024; i++; }
        string n = i == 0 || v >= 100 ? v.ToString("0") : v >= 10 ? v.ToString("0.#") : v.ToString("0.##");
        return $"{n} {Units[i]}";
    }

    public static string Ago(DateTime when)
    {
        var d = DateTime.Now - when;
        if (d.TotalDays < 1) return "today";
        if (d.TotalDays < 2) return "yesterday";
        if (d.TotalDays < 14) return $"{(int)d.TotalDays} days ago";
        if (d.TotalDays < 60) return $"{(int)(d.TotalDays / 7)} weeks ago";
        if (d.TotalDays < 365) return $"{(int)(d.TotalDays / 30)} months ago";
        var y = (int)(d.TotalDays / 365);
        return y == 1 ? "a year ago" : $"{y} years ago";
    }
}
