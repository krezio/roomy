namespace Roomy.Core;

public enum Safety { Safe, Review }

public enum CleanAction
{
    /// <summary>Delete files (for folders: delete the contents, keep the folder).</summary>
    Delete,
    EmptyRecycleBin,
    /// <summary>Hand off to Steam's own uninstaller.</summary>
    SteamUninstall,
    /// <summary>Only Windows can remove it safely, so we open Settings.</summary>
    OpenSettings,
}

public sealed class CleanupItem
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Paths { get; init; }
    public string Path => Paths.Count > 0 ? Paths[0] : "";
    public long Size { get; set; }
    public int FileCount { get; set; }
    public bool IsFile { get; init; }
    public string? Detail { get; init; }
    /// <summary>Only files older than this are measured and deleted.</summary>
    public TimeSpan? MinAge { get; init; }
    public string? AppId { get; init; }
}

public sealed class CleanupCategory
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Glyph { get; init; }
    public Safety Safety { get; init; } = Safety.Safe;
    public CleanAction Action { get; init; } = CleanAction.Delete;
    public bool SelectedByDefault { get; init; }
    public bool ShowItems { get; init; } = true;
    public bool Blocked { get; set; }
    public string? Note { get; set; }
    public List<CleanupItem> Items { get; } = [];
    public long Size => Items.Sum(i => i.Size);
}

/// <summary>Segoe Fluent Icons / MDL2 code points.</summary>
public static class G
{
    public const string Delete = "";
    public const string Clock = "";
    public const string Globe = "";
    public const string Apps = "";
    public const string Color = "";
    public const string Bug = "";
    public const string Update = "";
    public const string Package = "";
    public const string Download = "";
    public const string Game = "";
    public const string History = "";
    public const string Folder = "";
    public const string Document = "";
    public const string Shield = "";
    public const string Check = "";
    public const string ChevronDown = "";
    public const string ChevronUp = "";
    public const string ChevronRight = "";
}

/// <summary>The fixed copy for every category, shared by the real scanner and demo mode.</summary>
public static class Catalog
{
    public static CleanupCategory Create(string id) => id switch
    {
        "recycle" => new CleanupCategory
        {
            Id = id, Title = "Recycle Bin", Glyph = G.Delete,
            Description = "Stuff you already deleted. It still takes up space until the bin is emptied.",
            Action = CleanAction.EmptyRecycleBin, SelectedByDefault = true, ShowItems = false,
        },
        "temp" => new CleanupCategory
        {
            Id = id, Title = "Temporary files", Glyph = G.Clock,
            Description = "Leftovers apps create while they work and forget to clean up. Only files older than a day.",
            SelectedByDefault = true,
        },
        "browser" => new CleanupCategory
        {
            Id = id, Title = "Browser cache", Glyph = G.Globe,
            Description = "Saved copies of websites. You stay logged in — sites may just load a little slower the first time.",
            SelectedByDefault = true,
        },
        "apps" => new CleanupCategory
        {
            Id = id, Title = "App caches", Glyph = G.Apps,
            Description = "Cached data from Discord, VS Code, Slack and friends. They rebuild it on their own when needed.",
            SelectedByDefault = true,
        },
        "shaders" => new CleanupCategory
        {
            Id = id, Title = "Graphics shader cache", Glyph = G.Color,
            Description = "Game graphics your GPU pre-built. Safe to clear — games rebuild it, but the next launch may stutter a bit.",
            SelectedByDefault = false,
        },
        "crash" => new CleanupCategory
        {
            Id = id, Title = "Crash reports", Glyph = G.Bug,
            Description = "Reports Windows saves whenever an app crashes. Only useful to developers.",
            SelectedByDefault = true,
        },
        "windows" => new CleanupCategory
        {
            Id = id, Title = "Windows Update leftovers", Glyph = G.Update,
            Description = "Update files Windows already installed but keeps around just in case, plus Windows' own temp files.",
            SelectedByDefault = true,
        },
        "installers" => new CleanupCategory
        {
            Id = id, Title = "Old installers", Glyph = G.Package, Safety = Safety.Review,
            Description = "Setup files in your Downloads you've most likely already run. Only ones older than a week.",
        },
        "downloads" => new CleanupCategory
        {
            Id = id, Title = "Big old downloads", Glyph = G.Download, Safety = Safety.Review,
            Description = "Large files in your Downloads you haven't touched in over a month — old videos, ISOs, zips.",
        },
        "games" => new CleanupCategory
        {
            Id = id, Title = "Games you haven't played", Glyph = G.Game, Safety = Safety.Review,
            Description = "Steam games you haven't opened in 2+ months. Uninstall through Steam — you can always reinstall.",
            Action = CleanAction.SteamUninstall,
        },
        "winold" => new CleanupCategory
        {
            Id = id, Title = "Previous Windows version", Glyph = G.History, Safety = Safety.Review,
            Description = "A backup of your old Windows from the last big update. You only need it if you want to roll back.",
            Action = CleanAction.OpenSettings, ShowItems = false,
        },
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown category"),
    };
}
