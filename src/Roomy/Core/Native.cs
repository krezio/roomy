using System.Runtime.InteropServices;

namespace Roomy.Core;

internal static class Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SHQUERYRBINFO
    {
        public int cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    public const uint SHERB_NOCONFIRMATION = 0x1;
    public const uint SHERB_NOPROGRESSUI = 0x2;
    public const uint SHERB_NOSOUND = 0x4;

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    public static readonly Guid DownloadsFolder = new("374DE290-123F-4565-9164-39C4925E467B");

    public static string? KnownFolder(Guid id)
    {
        try
        {
            if (SHGetKnownFolderPath(id, 0, IntPtr.Zero, out var p) == 0)
            {
                try { return Marshal.PtrToStringUni(p); }
                finally { Marshal.FreeCoTaskMem(p); }
            }
        }
        catch { }
        return null;
    }

    public static (long Size, long Items) QueryRecycleBin()
    {
        try
        {
            var info = new SHQUERYRBINFO { cbSize = Marshal.SizeOf<SHQUERYRBINFO>() };
            return SHQueryRecycleBin(null, ref info) == 0 ? (info.i64Size, info.i64NumItems) : (0, 0);
        }
        catch { return (0, 0); }
    }
}
