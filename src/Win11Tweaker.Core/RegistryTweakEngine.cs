using Microsoft.Win32;

namespace Win11Tweaker.Core;

public sealed record Change(RegistryWrite Write, string? Before, string? After);

public static class RegistryTweakEngine
{
    public static TweakState Read(TweakDefinition tweak, int currentBuild)
    {
        if (!tweak.AppliesTo.Includes(currentBuild))
            return TweakState.Unsupported;

        var on = 0;
        var off = 0;
        var applicable = 0;

        foreach (var write in tweak.Writes)
        {
            if (Skip(write))
                continue;

            applicable++;
            switch (StateOf(write))
            {
                case TweakState.On: on++; break;
                case TweakState.Off: off++; break;
                default: return TweakState.Drifted;
            }
        }

        if (applicable == 0) return TweakState.Off;
        if (on == applicable) return TweakState.On;
        if (off == applicable) return TweakState.Off;

        return TweakState.Drifted;
    }

    public static IReadOnlyList<Change> Plan(TweakDefinition tweak, bool turnOn)
    {
        var plan = new List<Change>();

        foreach (var write in tweak.Writes)
        {
            if (Skip(write))
                continue;

            var before = write.AbsentWhenOn ? Presence(write) : Describe(CurrentValue(write), write);
            var after = write.PresenceOnly
                ? (turnOn ? "(ключ есть)" : "(ключа нет)")
                : write.AbsentWhenOn
                    ? (turnOn ? "(ключа нет)" : "(ключ есть)")
                    : Describe(turnOn ? write.OnValue : write.OffValue, write);

            if (before != after)
                plan.Add(new Change(write, before, after));
        }

        return plan;
    }

    public static IReadOnlyList<Change> Apply(TweakDefinition tweak, bool turnOn)
    {
        var done = new List<Change>();

        foreach (var write in tweak.Writes)
        {
            if (Skip(write))
                continue;

            var before = write.AbsentWhenOn ? Presence(write) : Describe(CurrentValue(write), write);

            if (write.PresenceOnly)
            {
                if (turnOn)
                    CreateShimKey(write);
                else
                    DeleteKey(write);
            }
            else if (write.AbsentWhenOn)
            {
                if (turnOn)
                    DeleteKey(write);
                else
                    RestoreKey(write);
            }
            else if (turnOn)
            {
                Write(write, write.OnValue);
            }
            else if (write.OffValue is null)
            {
                DeleteValue(write);
            }
            else
            {
                Write(write, write.OffValue);
            }

            var after = write.AbsentWhenOn ? Presence(write) : Describe(CurrentValue(write), write);
            if (before != after)
                done.Add(new Change(write, before, after));
        }

        return done;
    }

    public static void Restore(RegistryWrite write, string? before)
    {
        if (write.PresenceOnly)
        {
            if (before == "(ключ есть)")
                CreateShimKey(write);
            else
                DeleteKey(write);
            return;
        }

        if (write.AbsentWhenOn)
        {
            if (before == "(ключ есть)")
                RestoreKey(write);
            else
                DeleteKey(write);
            return;
        }

        if (before is null or "(не задано)")
        {
            DeleteValue(write);
            return;
        }

        Write(write, Parse(before, write.Kind));
    }

    static TweakState StateOf(RegistryWrite write)
    {
        if (write.PresenceOnly)
            return OpenKey(write, writable: false) is null ? TweakState.Off : TweakState.On;

        if (write.AbsentWhenOn)
            return OpenKey(write, writable: false) is null ? TweakState.On : TweakState.Off;

        var current = CurrentValue(write);

        if (Same(current, write.OnValue)) return TweakState.On;
        if (current is null || Same(current, write.OffValue)) return TweakState.Off;

        return TweakState.Drifted;
    }

    static bool Skip(RegistryWrite write)
    {
        if (!write.SkipIfKeyAbsent)
            return false;
        using var key = Root(write).OpenSubKey(write.Key, writable: false);
        return key is null;
    }

    static RegistryKey Root(RegistryWrite write)
        => write.Root == RegistryRoot.CurrentUser ? Registry.CurrentUser : Registry.LocalMachine;

    static RegistryKey? OpenKey(RegistryWrite write, bool writable)
        => Root(write).OpenSubKey(write.Key, writable);

    static object? CurrentValue(RegistryWrite write)
    {
        using var key = OpenKey(write, writable: false);
        return key?.GetValue(write.Name ?? string.Empty, null);
    }

    static void Write(RegistryWrite write, object value)
    {
        using var key = Root(write).CreateSubKey(write.Key, writable: true);
        key.SetValue(write.Name ?? string.Empty, value, write.Kind);
    }

    static void DeleteValue(RegistryWrite write)
    {
        using var key = OpenKey(write, writable: true);
        key?.DeleteValue(write.Name ?? string.Empty, throwOnMissingValue: false);
    }

    static void CreateShimKey(RegistryWrite write)
    {
        using var key = Root(write).CreateSubKey(write.Key, writable: true);
        key.SetValue(string.Empty, write.OnValue, write.Kind);
    }

    static void DeleteKey(RegistryWrite write)
    {
        Root(write).DeleteSubKeyTree(write.Key, throwOnMissingSubKey: false);
    }

    static void RestoreKey(RegistryWrite write)
    {
        using var key = Root(write).CreateSubKey(write.Key, writable: true);
        if (write.OffValue is not null)
            key.SetValue(string.Empty, write.OffValue, RegistryValueKind.String);
    }

    static string Presence(RegistryWrite write)
    {
        using var key = OpenKey(write, writable: false);
        return key is null ? "(ключа нет)" : "(ключ есть)";
    }

    static bool Same(object? current, object? expected)
    {
        if (current is null || expected is null)
            return current is null && expected is null;

        if (current is int a && expected is int b)
            return a == b;

        return string.Equals(current.ToString(), expected.ToString(), StringComparison.Ordinal);
    }

    static string Describe(object? value, RegistryWrite write)
    {
        if (write.PresenceOnly)
            return OpenKey(write, writable: false) is null ? "(ключа нет)" : "(ключ есть)";

        if (value is null)
            return "(не задано)";

        var text = value.ToString() ?? string.Empty;
        return text.Length == 0 ? "(пусто)" : text;
    }

    static object Parse(string text, RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.DWord => int.TryParse(text, out var number) ? number : 0,
        RegistryValueKind.QWord => long.TryParse(text, out var big) ? big : 0L,
        _ => text == "(пусто)" ? string.Empty : text
    };
}
