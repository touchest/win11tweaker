using System.IO;
using Microsoft.Win32;

namespace Win11Tweaker.App.Interop;

public enum AutoSource
{
    CurrentUserRun,
    LocalMachineRun,
    LocalMachineRun32,
    StartupFolder,
    ScheduledTask
}

public sealed record AutostartEntry(
    string Name, string Command, string Path, string Publisher, bool Enabled, bool Trusted, AutoSource Source);

public static class Autostart
{
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunKey32 = @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run";
    const string ApprovedRun = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    const string ApprovedRun32 = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32";
    const string ApprovedFolder = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

    public static List<AutostartEntry> Scan(TrustList trust)
    {
        var entries = new List<AutostartEntry>();

        entries.AddRange(ReadRun(Registry.CurrentUser, RunKey, ApprovedRun, AutoSource.CurrentUserRun, trust));
        entries.AddRange(ReadRun(Registry.LocalMachine, RunKey, ApprovedRun, AutoSource.LocalMachineRun, trust));
        entries.AddRange(ReadRun(Registry.LocalMachine, RunKey32, ApprovedRun32, AutoSource.LocalMachineRun32, trust));
        entries.AddRange(ReadStartupFolder(trust));
        entries.AddRange(ReadScheduledTasks(trust));

        return entries;
    }

    public static void Remove(AutostartEntry entry)
    {
        if (entry.Source == AutoSource.ScheduledTask)
        {
            TaskManager.Delete(entry.Name);
            return;
        }

        var (hive, runPath, approvedPath) = entry.Source switch
        {
            AutoSource.CurrentUserRun => (Registry.CurrentUser, RunKey, ApprovedRun),
            AutoSource.LocalMachineRun => (Registry.LocalMachine, RunKey, ApprovedRun),
            AutoSource.LocalMachineRun32 => (Registry.LocalMachine, RunKey32, ApprovedRun32),
            _ => (Registry.CurrentUser, string.Empty, ApprovedFolder)
        };

        if (runPath.Length == 0)
            return;

        using (var run = hive.OpenSubKey(runPath, writable: true))
            run?.DeleteValue(entry.Name, throwOnMissingValue: false);

        using var approved = hive.OpenSubKey(approvedPath, writable: true);
        approved?.DeleteValue(entry.Name, throwOnMissingValue: false);
    }

    public static void SetEnabled(AutostartEntry entry, bool enabled)
    {
        if (entry.Source == AutoSource.ScheduledTask)
        {
            TaskManager.SetDisabled(entry.Name, !enabled);
            return;
        }

        var (hive, approvedPath) = entry.Source switch
        {
            AutoSource.CurrentUserRun => (Registry.CurrentUser, ApprovedRun),
            AutoSource.LocalMachineRun => (Registry.LocalMachine, ApprovedRun),
            AutoSource.LocalMachineRun32 => (Registry.LocalMachine, ApprovedRun32),
            _ => (Registry.CurrentUser, ApprovedFolder)
        };

        using var approved = hive.CreateSubKey(approvedPath, writable: true);

        var buffer = new byte[12];
        buffer[0] = (byte)(enabled ? 2 : 3);
        if (!enabled)
            BitConverter.GetBytes(DateTime.Now.ToFileTime()).CopyTo(buffer, 4);

        approved.SetValue(entry.Name, buffer, RegistryValueKind.Binary);
    }

    static IEnumerable<AutostartEntry> ReadRun(RegistryKey hive, string runPath, string approvedPath, AutoSource source, TrustList trust)
    {
        using var run = hive.OpenSubKey(runPath);
        if (run is null)
            yield break;

        using var approved = hive.OpenSubKey(approvedPath);

        foreach (var name in run.GetValueNames())
        {
            if (name.Length == 0)
                continue;

            var command = run.GetValue(name)?.ToString() ?? string.Empty;
            var path = ResolveExe(command);
            var (signed, signer) = SignatureVerifier.Check(path);
            var enabled = IsEnabled(approved, name);
            var trusted = signed || trust.IsException(name);

            yield return new AutostartEntry(name, command, path, signer ?? "без подписи",
                enabled, trusted, source);
        }
    }

    static IEnumerable<AutostartEntry> ReadStartupFolder(TrustList trust)
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        if (!Directory.Exists(folder))
            yield break;

        using var approved = Registry.CurrentUser.OpenSubKey(ApprovedFolder);

        foreach (var shortcut in Directory.EnumerateFiles(folder, "*.lnk"))
        {
            var name = System.IO.Path.GetFileName(shortcut);
            var enabled = IsEnabled(approved, name);
            var trusted = trust.IsException(name);

            yield return new AutostartEntry(name, shortcut, shortcut, "ярлык", enabled, trusted, AutoSource.StartupFolder);
        }
    }

    static IEnumerable<AutostartEntry> ReadScheduledTasks(TrustList trust)
    {
        foreach (var task in TaskManager.QueryRoot())
        {
            var path = ResolveExe(task.Command);
            var (signed, signer) = SignatureVerifier.Check(path);

            yield return new AutostartEntry(task.Name, task.Command, path,
                signer ?? "без подписи", !task.Disabled,
                signed || trust.IsException(task.Name), AutoSource.ScheduledTask);
        }
    }

    static bool IsEnabled(RegistryKey? approved, string name)
    {
        if (approved?.GetValue(name) is not byte[] state || state.Length == 0)
            return true;

        return (state[0] & 1) == 0;
    }

    static string ResolveExe(string command)
    {
        command = command.Trim();
        if (command.Length == 0)
            return string.Empty;

        if (command[0] == '"')
        {
            var end = command.IndexOf('"', 1);
            return end > 0 ? command[1..end] : command;
        }

        var exe = command.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (exe > 0)
            return command[..(exe + 4)];

        var space = command.IndexOf(' ');
        return space > 0 ? command[..space] : command;
    }
}
