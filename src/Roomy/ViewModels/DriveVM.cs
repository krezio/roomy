using System.Windows.Media;
using Roomy.Core;

namespace Roomy.ViewModels;

public sealed class DriveVM : ObservableObject
{
    public DriveVM(string root, string label, long total, long free)
    {
        Root = root;
        Label = label;
        Update(total, free);
    }

    public string Root { get; }
    public string Label { get; }
    public string Letter => Root.TrimEnd('\\');
    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? "Local Disk" : Label;
    public string Title => $"{DisplayName} ({Letter})";

    public long Total { get; private set; }
    public long Free { get; private set; }
    public long Used => Math.Max(0, Total - Free);
    public double UsedFraction => Total > 0 ? (double)Used / Total : 0;

    public string Subtitle => $"{Format.Bytes(Total)} drive";
    public string FreeNumber => Format.Bytes(Free);
    public string FreeRest => $"free of {Format.Bytes(Total)}";
    public string FreeShort => $"{Format.Bytes(Free)} free";
    public string UsedText => $"{Format.Bytes(Used)} used";
    public string PercentText => $"{UsedFraction * 100:0}% full";
    public string StatusText => UsedFraction >= .9 ? "Almost full" : UsedFraction >= .75 ? "Getting full" : "Looking good";

    string Tone => UsedFraction >= .9 ? "Danger" : UsedFraction >= .75 ? "Warn" : "Accent";
    public Brush StatusBrush => Res.Brush(Tone + "Brush");
    public Brush StatusBg => Res.Brush(Tone + "SoftBrush");
    public Brush BarBrush => Res.Brush(Tone + "Gradient");

    bool _spaceSelected;
    public bool IsSpaceSelected { get => _spaceSelected; set => Set(ref _spaceSelected, value); }

    public void Update(long total, long free)
    {
        Total = total;
        Free = free;
        RaiseAll();
    }
}
