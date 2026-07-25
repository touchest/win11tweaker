using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace Win11Tweaker.App.Interop;

public static class EdgeUpdateRemoval
{
    static readonly string[] ProcessNames =
    [
        "MicrosoftEdgeUpdate", "MicrosoftEdgeUpdateCore", "MicrosoftEdgeUpdateBroker",
        "MicrosoftEdgeUpdateComRegisterShell64", "CopilotUpdate", "msedgewebview2"
    ];

    static readonly string[] ServiceNames = ["edgeupdate", "edgeupdatem"];

    const string PolicyKey = @"SOFTWARE\Policies\Microsoft\EdgeUpdate";

    static readonly string[] ProductGuids =
    [
        "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}",
        "{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}",
        "{2CD8A007-E189-409D-A2C8-9AF4EF3C72AA}"
    ];

    static readonly string[] WebViewUninstallKeys =
    [
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft EdgeWebView",
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Microsoft EdgeWebView"
    ];

    public sealed record Report(int Processes, int Services, int Tasks, bool WebView, bool Policy, int Folders, string? Blocked)
    {
        public string Describe()
        {
            if (Blocked is not null)
                return Blocked;

            var parts = new List<string>();
            if (WebView) parts.Add("WebView2 удалён");
            if (Services > 0) parts.Add(Services == 1 ? "1 служба" : Services + " службы");
            if (Tasks > 0) parts.Add(Tasks == 1 ? "1 задача" : Tasks + " задачи");
            if (Policy) parts.Add("самовосстановление заблокировано");
            if (Folders > 0) parts.Add(Folders == 1 ? "папка удалена" : Folders + " папки удалены");

            if (parts.Count == 0)
                return "Edge и его обновление уже вырезаны - удалять нечего.";

            return "Готово: " + string.Join(", ", parts) + ".";
        }
    }

    public static bool Present()
    {
        if (ServiceNames.Any(ServiceControl.Exists))
            return true;

        if (BrandFolders(includeEdgeCore: false).Any(Directory.Exists))
            return true;

        using var key = Registry.LocalMachine.OpenSubKey(PolicyKey);
        return key?.GetValue("UpdateDefault") is not 0;
    }

    public static Report Run(bool deleteFolder)
    {
        if (!Elevation.IsAdmin)
            return new Report(0, 0, 0, false, false, 0,
                "Нужны права администратора. Перезапустите Win11 Tweaker от имени администратора.");

        var webView = RemoveWebView2();

        var services = StopAndDeleteServices();
        var processes = KillProcesses();
        var tasks = DeleteTasks();
        var policy = WritePolicy();
        var folders = deleteFolder ? DeleteFolders() : 0;

        return new Report(processes, services, tasks, webView, policy, folders, null);
    }

    static bool RemoveWebView2()
    {
        var removed = false;
        foreach (var setup in WebViewSetups())
            removed |= RunUninstaller(setup);
        return removed;
    }

    static IEnumerable<string> WebViewSetups()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var keyPath in WebViewUninstallKeys)
        {
            using var key = Registry.LocalMachine.OpenSubKey(keyPath);
            if (key?.GetValue("UninstallString") is string s && ExeFromCommand(s) is { } exe
                && File.Exists(exe) && seen.Add(exe))
                yield return exe;
        }

        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        foreach (var brand in new[] { "EdgeWebView", "EdgeCore" })
        {
            var appDir = Path.Combine(pf, "Microsoft", brand, "Application");
            foreach (var ver in SafeSubdirs(appDir))
            {
                var setup = Path.Combine(ver, "Installer", "setup.exe");
                if (File.Exists(setup) && seen.Add(setup))
                    yield return setup;
            }
        }
    }

    static bool RunUninstaller(string setupExe)
    {
        try
        {
            var p = Process.Start(new ProcessStartInfo(setupExe,
                "--uninstall --msedgewebview --system-level --verbose-logging --force-uninstall")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            if (p is null)
                return false;
            p.WaitForExit(120_000);
            return p.HasExited;
        }
        catch (Exception) { return false; }
    }

    static string? ExeFromCommand(string cmd)
    {
        cmd = cmd.Trim();
        if (cmd.Length == 0)
            return null;
        if (cmd[0] == '"')
        {
            var end = cmd.IndexOf('"', 1);
            return end > 1 ? cmd[1..end] : null;
        }
        var space = cmd.IndexOf(' ');
        return space > 0 ? cmd[..space] : cmd;
    }

    static int KillProcesses()
    {
        var killed = 0;
        foreach (var name in ProcessNames)
        {
            foreach (var proc in Process.GetProcessesByName(name))
            {
                try
                {
                    proc.Kill(entireProcessTree: true);
                    proc.WaitForExit(5_000);
                    killed++;
                }
                catch (Exception) {  }
                finally { proc.Dispose(); }
            }
        }
        return killed;
    }

    static int StopAndDeleteServices()
    {
        var deleted = 0;

        var scm = OpenSCManager(null, null, SC_MANAGER_ALL_ACCESS);
        if (scm == IntPtr.Zero)
            return 0;

        try
        {
            foreach (var name in ServiceNames)
            {
                var svc = OpenService(scm, name, SERVICE_STOP | SERVICE_QUERY_STATUS | DELETE);
                if (svc == IntPtr.Zero)
                    continue;

                try
                {
                    var status = new SERVICE_STATUS();
                    ControlService(svc, SERVICE_CONTROL_STOP, ref status);
                    WaitForStop(svc);

                    if (DeleteService(svc))
                        deleted++;
                }
                finally { CloseServiceHandle(svc); }
            }
        }
        finally { CloseServiceHandle(scm); }

        return deleted;
    }

    static void WaitForStop(IntPtr svc)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            if (!QueryServiceStatus(svc, out var status))
                return;
            if (status.dwCurrentState == SERVICE_STOPPED)
                return;
            System.Threading.Thread.Sleep(150);
        }
    }

    static int DeleteTasks()
    {
        var deleted = 0;
        foreach (var task in TaskManager.QueryRoot())
        {
            if (!task.Name.Contains("MicrosoftEdgeUpdate", StringComparison.OrdinalIgnoreCase))
                continue;

            TaskManager.Delete(task.Name);
            deleted++;
        }
        return deleted;
    }

    static bool WritePolicy()
    {
        try
        {
            using var key = Registry.LocalMachine.CreateSubKey(PolicyKey, writable: true);
            if (key is null)
                return false;

            key.SetValue("UpdateDefault", 0, RegistryValueKind.DWord);
            key.SetValue("InstallDefault", 0, RegistryValueKind.DWord);
            key.SetValue("AutoUpdateCheckPeriodMinutes", 0, RegistryValueKind.DWord);
            key.SetValue("DisableAutoUpdateChecksCheckboxValue", 1, RegistryValueKind.DWord);
            key.SetValue("UpdaterExperimentationAndConfigurationServiceControl", 0, RegistryValueKind.DWord);

            foreach (var guid in ProductGuids)
            {
                key.SetValue("Update" + guid, 0, RegistryValueKind.DWord);
                key.SetValue("Install" + guid, 0, RegistryValueKind.DWord);
            }

            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException or IOException)
        {
            return false;
        }
    }

    static int DeleteFolders()
    {
        var removed = 0;
        foreach (var dir in BrandFolders(includeEdgeCore: !EdgeBrowserInstalled()))
        {
            if (!Directory.Exists(dir))
                continue;
            try
            {
                Directory.Delete(dir, recursive: true);
                removed++;
            }
            catch (Exception)
            {
                if (!Directory.Exists(dir))
                    removed++;
            }
        }
        return removed;
    }

    static IEnumerable<string> BrandFolders(bool includeEdgeCore)
    {
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        yield return Path.Combine(pf, "Microsoft", "EdgeUpdate");
        yield return Path.Combine(pf, "Microsoft", "EdgeWebView");
        if (includeEdgeCore)
            yield return Path.Combine(pf, "Microsoft", "EdgeCore");
    }

    static bool EdgeBrowserInstalled()
    {
        var pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        return Directory.Exists(Path.Combine(pf, "Microsoft", "Edge", "Application"));
    }

    static IEnumerable<string> SafeSubdirs(string dir)
    {
        try { return Directory.Exists(dir) ? Directory.GetDirectories(dir) : []; }
        catch (Exception) { return []; }
    }

    const uint SC_MANAGER_ALL_ACCESS = 0xF003F;
    const uint SERVICE_STOP = 0x0020, SERVICE_QUERY_STATUS = 0x0004, DELETE = 0x00010000;
    const uint SERVICE_CONTROL_STOP = 0x00000001;
    const uint SERVICE_STOPPED = 1;

    [StructLayout(LayoutKind.Sequential)]
    struct SERVICE_STATUS
    {
        public uint dwServiceType, dwCurrentState, dwControlsAccepted, dwWin32ExitCode,
            dwServiceSpecificExitCode, dwCheckPoint, dwWaitHint;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr OpenSCManager(string? machine, string? database, uint access);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern IntPtr OpenService(IntPtr scm, string name, uint access);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool ControlService(IntPtr service, uint control, ref SERVICE_STATUS status);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool QueryServiceStatus(IntPtr service, out SERVICE_STATUS status);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool DeleteService(IntPtr service);

    [DllImport("advapi32.dll", SetLastError = true)]
    static extern bool CloseServiceHandle(IntPtr handle);
}
