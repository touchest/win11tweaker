using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Win11Tweaker.App.Interop;

public static class SafeModeDefenderKill
{
    const string TaskName = "Win11TweakerSafeModeDefender";
    const string SafeBootScheduleKey = @"SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal\Schedule";

    static string ScriptPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Win11Tweaker", "safemode-defender.cmd");

    public static void CleanupResidue()
    {
        if (!ResiduePresent())
            return;

        Run("bcdedit", "/deletevalue {current} safeboot");
        Run("schtasks", $"/delete /tn {TaskName} /f");
        Run("reg", $"delete \"HKLM\\{SafeBootScheduleKey}\" /f");

        try { File.Delete(ScriptPath); } catch (IOException) { }
    }

    static bool ResiduePresent()
    {
        using var key = Registry.LocalMachine.OpenSubKey(SafeBootScheduleKey);
        return key is not null || File.Exists(ScriptPath);
    }

    static void Run(string exe, string args)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            });
            process?.WaitForExit();
        }
        catch (Exception)
        {
        }
    }
}
