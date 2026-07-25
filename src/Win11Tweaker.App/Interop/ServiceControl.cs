using System.Collections.Generic;
using System.Linq;
using System.Security;
using Microsoft.Win32;

namespace Win11Tweaker.App.Interop;

public static class ServiceControl
{
    const string Services = @"SYSTEM\CurrentControlSet\Services";

    public const int Disabled = 4;
    public const int Manual = 3;

    public static bool Exists(string name)
    {
        using var key = Registry.LocalMachine.OpenSubKey(Services + "\\" + name);
        return key is not null;
    }

    public static int? StartOf(string name)
    {
        using var key = Registry.LocalMachine.OpenSubKey(Services + "\\" + name);
        return key?.GetValue("Start") as int?;
    }

    public static bool IsDisabled(string name) => StartOf(name) == Disabled;

    public static ISet<string> EnabledServices()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var root = Registry.LocalMachine.OpenSubKey(Services);
        if (root is null)
            return set;

        foreach (var name in root.GetSubKeyNames())
        {
            using var key = root.OpenSubKey(name);
            if (key?.GetValue("Start") is int start && start != Disabled)
                set.Add(name);
        }

        return set;
    }

    public static (int Done, int Failed) ApplyStarts(IReadOnlyList<(string Name, int Value)> items)
    {
        var done = 0;
        var denied = new List<(string, int)>();

        foreach (var (name, value) in items)
        {
            try
            {
                if (Write(name, value))
                    done++;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or SecurityException)
            {
                denied.Add((name, value));
            }
        }

        var failed = 0;
        if (denied.Count > 0)
        {
            try
            {
                TrustedInstaller.RunAs(() =>
                {
                    foreach (var (name, value) in denied)
                    {
                        try { if (Write(name, value)) done++; else failed++; }
                        catch (Exception) { failed++; }
                    }
                });
            }
            catch (Exception)
            {
                failed += denied.Count;
            }
        }

        return (done, failed);
    }

    static bool Write(string name, int value)
    {
        using var key = Registry.LocalMachine.OpenSubKey(Services + "\\" + name, writable: true);
        if (key is null)
            return false;
        key.SetValue("Start", value, RegistryValueKind.DWord);
        return true;
    }
}
