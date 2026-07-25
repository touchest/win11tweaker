using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Win11Tweaker.App.Interop;

public static class AppxManager
{
    static HashSet<string>? installed;

    public static bool IsInstalled(string name)
    {
        installed ??= Query();
        return installed.Contains(name.ToLowerInvariant());
    }

    public static void Remove(string name)
    {
        RunPowerShell($"Get-AppxPackage -Name '{name}' | Remove-AppxPackage -ErrorAction SilentlyContinue");
        RunPowerShell(
            "Get-AppxProvisionedPackage -Online | " +
            $"Where-Object {{ $_.DisplayName -eq '{name}' }} | " +
            "Remove-AppxProvisionedPackage -Online -ErrorAction SilentlyContinue");

        installed = null;
    }

    public static void UnpinFromTaskbar(string match)
    {
        var pinned = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            @"Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar");

        if (!Directory.Exists(pinned))
            return;

        foreach (var link in Directory.GetFiles(pinned, "*.lnk"))
        {
            if (!Path.GetFileNameWithoutExtension(link).Contains(match, StringComparison.OrdinalIgnoreCase))
                continue;

            try { File.Delete(link); } catch (IOException) { }
        }
    }

    static HashSet<string> Query()
    {
        var set = new HashSet<string>();

        foreach (var line in RunPowerShell("Get-AppxPackage | Select-Object -ExpandProperty Name").Split('\n'))
        {
            var name = line.Trim();
            if (name.Length > 0)
                set.Add(name.ToLowerInvariant());
        }

        return set;
    }

    static string RunPowerShell(string command)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("powershell.exe",
                $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{command}\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process is null)
                return string.Empty;

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
