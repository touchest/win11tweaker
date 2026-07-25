using System.Windows.Media;

namespace Win11Tweaker.App.Shell;

public abstract class NavPage : ObservableBase
{
    const string Iris = "#6E7BE8";

    protected NavPage(string title, string group, string accentHex)
    {
        Title = title;
        Group = group;
        var hue = (Color)ColorConverter.ConvertFromString(Iris)!;
        Accent = Frozen(hue);
        AccentSoft = Frozen(Color.FromArgb(0x33, hue.R, hue.G, hue.B));
        AccentInk = Frozen(Readable(hue));
    }

    public string Title { get; }

    public string Group { get; }

    public SolidColorBrush Accent { get; }

    public SolidColorBrush AccentSoft { get; }

    public SolidColorBrush AccentInk { get; }

    public virtual string Badge => string.Empty;

    public override string ToString() => Title;

    protected static SolidColorBrush Frozen(Color hue)
    {
        var brush = new SolidColorBrush(hue);
        brush.Freeze();
        return brush;
    }

    static Color Readable(Color on)
        => 0.299 * on.R + 0.587 * on.G + 0.114 * on.B > 140
            ? Color.FromRgb(0x10, 0x10, 0x14)
            : Colors.White;
}
