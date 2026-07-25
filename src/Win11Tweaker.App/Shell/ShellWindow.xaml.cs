using System.Windows;
using Win11Tweaker.App.Interop;

namespace Win11Tweaker.App.Shell;

public partial class ShellWindow : Window
{
    public ShellWindow()
    {
        InitializeComponent();

        StateChanged += (_, _) =>
        {
            if (WindowState == WindowState.Maximized)
                WindowState = WindowState.Normal;
        };

        Closing += (_, e) =>
        {
            var app = (App)Application.Current;
            if (app.IsExiting)
                return;
            e.Cancel = true;
            Hide();
            app.OnHiddenToTray();
        };
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowDressing.ApplyDarkFrame(this);
        FitToWorkArea();
    }

    void FitToWorkArea()
    {
        var maxWidth = SystemParameters.WorkArea.Width - 80;
        var maxHeight = SystemParameters.WorkArea.Height - 60;

        if (Width > maxWidth)
            Width = Math.Max(MinWidth, maxWidth);

        if (Height > maxHeight)
            Height = Math.Max(MinHeight, maxHeight);

        Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - Width) / 2;
        Top = SystemParameters.WorkArea.Top + (SystemParameters.WorkArea.Height - Height) / 2;
    }

    void OpenSettings(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel vm)
            vm.Selected = vm.Settings;
    }

    void Collapse(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    void Dismiss(object sender, RoutedEventArgs e) => Close();
}
