using System.IO;
using System.Windows;
using Win11Tweaker.App.Shell;

namespace Win11Tweaker.App;

public partial class App : Application
{
    Shell.TrayIcon? tray;
    bool hideHintShown;

#if DEBUG
    FileSystemWatcher? devQuit;
#endif

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            LogCrash(args.Exception);
            args.Handled = true;
        };

        tray = new Shell.TrayIcon(RestoreFromTray, ExitApp);

#if DEBUG

        var signal = Path.Combine(Path.GetTempPath(), "win11tweaker-quit");
        if (File.Exists(signal))
        {
            try { File.Delete(signal); } catch (IOException) { }
        }

        devQuit = new FileSystemWatcher(Path.GetTempPath(), "win11tweaker-quit") { EnableRaisingEvents = true };
        devQuit.Created += (_, _) => Dispatcher.Invoke(Shutdown);
#endif
    }

    public bool IsExiting { get; private set; }

    public void ExitApp()
    {
        IsExiting = true;
        Shutdown();
    }

    public void OnHiddenToTray()
    {
        tray?.Show(
            hideHintShown ? null : "Win11 Tweaker свёрнут",
            hideHintShown ? null : "Окно свёрнуто в трей. Значок в трее вернёт окно или закроет программу.");
        hideHintShown = true;
    }

    void RestoreFromTray()
    {
        tray?.Hide();
        if (MainWindow is not { } window)
            return;

        window.Show();
        window.WindowState = WindowState.Normal;

        window.Activate();
        window.Topmost = true;
        window.Topmost = false;
        window.Focus();
    }

    static void LogCrash(Exception ex)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Win11Tweaker");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "crash.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception)
        {
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        tray?.Dispose();
        base.OnExit(e);
    }
}
