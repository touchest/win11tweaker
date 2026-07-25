using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace Win11Tweaker.App.Interop;

public static class OneDriveRemoval
{
    static readonly string[] ProcessNames =
    [
        "OneDrive", "OneDriveSetup", "FileCoAuth", "FileSyncHelper", "OneDriveStandaloneUpdater"
    ];

    const string NavClsid = "{018D5C66-4533-4307-9B53-224DE2ED1FE6}";

    public sealed record Report(int Processes, bool Uninstalled, int Tasks, bool NavHidden, bool Autostart, string? Blocked)
    {
        public string Describe()
        {
            if (Blocked is not null)
                return Blocked;

            var parts = new List<string>();
            if (Uninstalled) parts.Add("деинсталлятор отработал");
            if (Tasks > 0) parts.Add(Tasks == 1 ? "1 задача" : Tasks + " задачи");
            if (NavHidden) parts.Add("значок убран из проводника");
            if (Autostart) parts.Add("автозапуск снят");

            if (parts.Count == 0)
                return "OneDrive не найден - удалять нечего.";

            return "Готово: " + string.Join(", ", parts) + ". Значок исчезнет после перезапуска проводника.";
        }
    }

    public static bool Present() => SetupPaths().Any(File.Exists) || Directory.Exists(UserFolder());

    public static Report Run()
    {
        var processes = KillProcesses();
        var uninstalled = RunUninstallers();
        var tasks = DeleteTasks();
        var nav = HideFromNavPane();
        var autostart = RemoveAutostart();

        return new Report(processes, uninstalled, tasks, nav, autostart, null);
    }

    static int KillProcesses()
    {
        var killed = 0;
        foreach (var name in ProcessNames)
            foreach (var proc in Process.GetProcessesByName(name))
            {
                try { proc.Kill(entireProcessTree: true); proc.WaitForExit(4_000); killed++; }
                catch (Exception) { }
                finally { proc.Dispose(); }
            }
        return killed;
    }

    static IEnumerable<string> SetupPaths()
    {
        var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        yield return Path.Combine(win, "System32", "OneDriveSetup.exe");
        yield return Path.Combine(win, "SysWOW64", "OneDriveSetup.exe");

        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(local, "Microsoft", "OneDrive", "OneDrive.exe");
        yield return Path.Combine(local, "Microsoft", "OneDrive", "Update", "OneDriveSetup.exe");
    }

    static string UserFolder() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "OneDrive");

    static bool RunUninstallers()
    {
        var any = false;
        foreach (var exe in SetupPaths().Where(File.Exists).Distinct())
        {
            try
            {
                var p = Process.Start(new ProcessStartInfo(exe, "/uninstall")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (p is not null)
                {
                    p.WaitForExit(90_000);
                    any = true;
                }
            }
            catch (Exception) { }
        }
        return any;
    }

    static int DeleteTasks()
    {
        var deleted = 0;
        foreach (var task in TaskManager.QueryRoot())
        {
            if (!task.Name.Contains("OneDrive", StringComparison.OrdinalIgnoreCase))
                continue;
            TaskManager.Delete(task.Name);
            deleted++;
        }
        return deleted;
    }

    static bool HideFromNavPane()
    {
        var ok = false;
        foreach (var root in new[]
        {
            @"SOFTWARE\Classes\CLSID\" + NavClsid,
            @"SOFTWARE\Wow6432Node\Classes\CLSID\" + NavClsid
        })
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(root, writable: true);
                key?.SetValue("System.IsPinnedToNameSpaceTree", 0, RegistryValueKind.DWord);
                ok = true;
            }
            catch (Exception) { }
        }
        return ok;
    }

    static bool RemoveAutostart()
    {
        try
        {
            using var run = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", writable: true);
            if (run?.GetValue("OneDrive") is null)
                return false;
            run.DeleteValue("OneDrive", throwOnMissingValue: false);
            return true;
        }
        catch (Exception) { return false; }
    }
}
