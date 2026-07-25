using System.Diagnostics;

namespace Win11Tweaker.App.Interop;

public readonly record struct TaskEntry(string Name, string Command, bool Disabled);

public static class TaskManager
{
    static Dictionary<string, bool>? states;

    public static bool Exists(string fullPath)
    {
        states ??= Query();
        return states.ContainsKey(fullPath.ToLowerInvariant());
    }

    public static bool IsDisabled(string fullPath)
    {
        states ??= Query();
        return states.TryGetValue(fullPath.ToLowerInvariant(), out var disabled) && disabled;
    }

    public static void SetDisabled(string fullPath, bool disable)
    {
        Run("schtasks.exe", $"/Change /TN \"{fullPath}\" /{(disable ? "DISABLE" : "ENABLE")}");
        states = null;
    }

    public static void Delete(string fullPath)
    {
        Run("schtasks.exe", $"/Delete /TN \"{fullPath}\" /F");
        states = null;
    }

    public static List<TaskEntry> QueryRoot()
    {
        var list = new List<TaskEntry>();

        foreach (var raw in Run("schtasks.exe", "/query /fo csv /nh /v").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length < 3 || !line.StartsWith('"'))
                continue;

            var fields = line.Trim('"').Split(["\",\""], StringSplitOptions.None);
            if (fields.Length < 9)
                continue;

            var name = fields[1].Trim();
            if (name.Length < 2 || name[0] != '\\' || name.IndexOf('\\', 1) >= 0)
                continue;

            var status = fields[3].Trim();
            if (status.Equals("Status", StringComparison.OrdinalIgnoreCase))
                continue;

            list.Add(new TaskEntry(name, fields[8].Trim(),
                status.Equals("Disabled", StringComparison.OrdinalIgnoreCase)));
        }

        return list;
    }

    static Dictionary<string, bool> Query()
    {
        var map = new Dictionary<string, bool>();

        foreach (var raw in Run("schtasks.exe", "/query /fo csv /nh").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length < 3 || !line.StartsWith('"'))
                continue;

            var fields = line.Trim('"').Split(["\",\""], StringSplitOptions.None);
            if (fields.Length < 3)
                continue;

            var path = fields[0].Trim();
            var status = fields[^1].Trim();
            if (path.Length == 0 || path.Equals("TaskName", StringComparison.OrdinalIgnoreCase))
                continue;

            map[path.ToLowerInvariant()] = status.Equals("Disabled", StringComparison.OrdinalIgnoreCase);
        }

        return map;
    }

    static string Run(string exe, string args)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(exe, args)
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
