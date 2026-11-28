using System.Globalization;
using Roomy.Core;
using Roomy.Localization;

namespace Roomy.Tests;

/// <summary>Byte counts and relative dates show up everywhere in the UI; pin their exact shapes.</summary>
public sealed class FormatTests : IDisposable
{
    const long MB = 1024L * 1024;

    // Format.Bytes follows the current culture (1,5 KB on a German PC is correct), and the
    // language catalogue follows the UI culture (Loc.Plural picks different keys per language),
    // so pin both for the assertions and give them back afterwards.
    readonly CultureInfo _previousCulture = CultureInfo.CurrentCulture;
    readonly CultureInfo _previousUiCulture = CultureInfo.CurrentUICulture;

    public FormatTests()
    {
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");
    }

    [Fact]
    public void ByteCountsStayReadableAtEveryScale()
    {
        Assert.Equal("0 B", Format.Bytes(0));
        Assert.Equal("0 B", Format.Bytes(-5));
        Assert.Equal("1 B", Format.Bytes(1));
        Assert.Equal("1023 B", Format.Bytes(1023));
        Assert.Equal("1 KB", Format.Bytes(1024));
        Assert.Equal("1.5 KB", Format.Bytes(1536));
        Assert.Equal("1.46 KB", Format.Bytes(1500));
        Assert.Equal("1 MB", Format.Bytes(MB));
        Assert.Equal("50 MB", Format.Bytes(50 * MB));
        Assert.Equal("500 MB", Format.Bytes(500 * MB));
        Assert.Equal("1 GB", Format.Bytes(1L << 30));
        Assert.Equal("1 TB", Format.Bytes(1L << 40));
        // Past the last unit the number simply keeps growing in TB.
        Assert.Equal("1024 TB", Format.Bytes(1L << 50));
    }

    [Fact]
    public void AgoPicksTheRightRelativeLabel()
    {
        // Compare against the same catalogue the app uses, so this passes in every language.
        var now = DateTime.Now;
        Assert.Equal(S.Today, Format.Ago(now));
        Assert.Equal(S.Yesterday, Format.Ago(now - TimeSpan.FromDays(1.2)));
        Assert.Equal(Loc.Format("AgoDays", 3), Format.Ago(now - TimeSpan.FromDays(3.5)));
        Assert.Equal(Loc.Format("AgoWeeks", 2), Format.Ago(now - TimeSpan.FromDays(20)));
        Assert.Equal(Loc.Format("AgoMonths", 3), Format.Ago(now - TimeSpan.FromDays(100)));
        Assert.Equal(S.YearAgo, Format.Ago(now - TimeSpan.FromDays(400)));
        Assert.Equal(Loc.Format("AgoYears", 2), Format.Ago(now - TimeSpan.FromDays(800)));
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _previousCulture;
        CultureInfo.CurrentUICulture = _previousUiCulture;
    }
}
