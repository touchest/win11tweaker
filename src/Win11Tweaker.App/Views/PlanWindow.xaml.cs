using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Win11Tweaker.App.Interop;

namespace Win11Tweaker.App.Views;

public sealed record PlanRow(string Tweak, string Target, string Before, string After);

public sealed record PlanContext(IReadOnlyList<PlanRow> Rows, Brush Accent);

public partial class PlanWindow : Window
{
    public PlanWindow(IReadOnlyList<PlanRow> rows, Brush accent)
    {
        InitializeComponent();
        DataContext = new PlanContext(rows, accent);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowDressing.ApplyDarkFrame(this);
    }

    void Accept(object sender, RoutedEventArgs e) => DialogResult = true;

    void Cancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
