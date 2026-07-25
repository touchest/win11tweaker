using Microsoft.Win32;

namespace Win11Tweaker.App.Interop;

public static class ServiceDependencies
{
    const string ServicesKey = @"SYSTEM\CurrentControlSet\Services";

    static IReadOnlyDictionary<string, List<string>>? reverse;

    public static IReadOnlyList<string> Dependents(string service)
    {
        reverse ??= BuildReverseMap();

        if (!reverse.TryGetValue(service.ToLowerInvariant(), out var dependents))
            return [];

        using var root = Registry.LocalMachine.OpenSubKey(ServicesKey);
        if (root is null)
            return [];

        var affected = new List<string>();

        foreach (var svc in dependents)
        {
            using var key = root.OpenSubKey(svc);
            if (key is null)
                continue;

            if (key.GetValue("Start") is int start && start == 4)
                continue;

            affected.Add(FriendlyName(key, svc));
        }

        return affected;
    }

    public static ISet<string> ForwardClosure(IEnumerable<string> seed)
    {
        var forward = BuildForwardMap();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new Stack<string>(seed);

        while (stack.Count > 0)
        {
            var svc = stack.Pop();
            if (!seen.Add(svc.ToLowerInvariant()))
                continue;

            if (forward.TryGetValue(svc.ToLowerInvariant(), out var deps))
                foreach (var dep in deps)
                    if (!seen.Contains(dep))
                        stack.Push(dep);
        }

        return seen;
    }

    static IReadOnlyDictionary<string, List<string>> BuildForwardMap()
    {
        var map = new Dictionary<string, List<string>>();

        using var root = Registry.LocalMachine.OpenSubKey(ServicesKey);
        if (root is null)
            return map;

        foreach (var name in root.GetSubKeyNames())
        {
            using var key = root.OpenSubKey(name);
            if (key?.GetValue("DependOnService") is not string[] deps)
                continue;

            map[name.ToLowerInvariant()] = [.. deps];
        }

        return map;
    }

    static IReadOnlyDictionary<string, List<string>> BuildReverseMap()
    {
        var map = new Dictionary<string, List<string>>();

        using var root = Registry.LocalMachine.OpenSubKey(ServicesKey);
        if (root is null)
            return map;

        foreach (var name in root.GetSubKeyNames())
        {
            using var key = root.OpenSubKey(name);
            if (key?.GetValue("DependOnService") is not string[] deps)
                continue;

            foreach (var dep in deps)
            {
                var target = dep.ToLowerInvariant();
                if (!map.TryGetValue(target, out var list))
                    map[target] = list = [];
                list.Add(name);
            }
        }

        return map;
    }

    static string FriendlyName(RegistryKey key, string fallback)
    {
        var display = key.GetValue("DisplayName") as string;
        return string.IsNullOrEmpty(display) || display.StartsWith('@') ? fallback : display;
    }
}
