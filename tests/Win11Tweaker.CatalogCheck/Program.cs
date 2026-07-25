using Microsoft.Win32;
using Win11Tweaker.App.Catalog;
using Win11Tweaker.Core;

var build = 22621;
var failures = 0;
var checks = 0;

void Check(string what, bool ok)
{
    checks++;
    if (!ok) { failures++; Console.WriteLine("      FAIL  " + what); }
}

static RegistryKey RootOf(RegistryWrite w)
    => w.Root == RegistryRoot.CurrentUser ? Registry.CurrentUser : Registry.LocalMachine;

static (bool KeyExists, bool ValueExists, object? Value) Snapshot(RegistryWrite w)
{
    using var key = RootOf(w).OpenSubKey(w.Key);
    if (key is null) return (false, false, null);
    if (w.PresenceOnly) return (true, true, key.GetValue(string.Empty));
    var name = w.Name ?? string.Empty;
    var present = key.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase);
    return (true, present, present ? key.GetValue(name) : null);
}

static void RestoreRaw(RegistryWrite w, (bool KeyExists, bool ValueExists, object? Value) snap)
{
    if (w.PresenceOnly)
    {
        if (snap.KeyExists)
        {
            using var made = RootOf(w).CreateSubKey(w.Key);
            made.SetValue(string.Empty, snap.Value ?? string.Empty, w.Kind);
        }
        else
        {
            RootOf(w).DeleteSubKeyTree(w.Key, throwOnMissingSubKey: false);
        }
        return;
    }

    if (!snap.KeyExists)
    {
        RootOf(w).DeleteSubKeyTree(w.Key, throwOnMissingSubKey: false);
        return;
    }

    using var key = RootOf(w).CreateSubKey(w.Key);
    var name = w.Name ?? string.Empty;

    if (snap.ValueExists)
        key.SetValue(name, snap.Value!, w.Kind);
    else
        key.DeleteValue(name, throwOnMissingValue: false);
}

static bool SameSnap((bool K, bool V, object? Val) a, (bool K, bool V, object? Val) b)
    => a.K == b.K && a.V == b.V
       && string.Equals(a.Val?.ToString(), b.Val?.ToString(), StringComparison.Ordinal);

var journalPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "win11tweaker-tweakcheck.json");
if (System.IO.File.Exists(journalPath)) System.IO.File.Delete(journalPath);

var all = InterfaceTweaks.Build().SelectMany(g => g.Tweaks).ToArray();
Console.WriteLine("твиков в каталоге: " + all.Length);
Console.WriteLine();

foreach (var tweak in all)
{
    var before = tweak.Writes.Select(Snapshot).ToArray();
    var startState = RegistryTweakEngine.Read(tweak, build);

    try
    {
        var onChanges = RegistryTweakEngine.Apply(tweak, turnOn: true);
        Check(tweak.Id + ": после включения состояние On",
            RegistryTweakEngine.Read(tweak, build) == TweakState.On);

        foreach (var w in tweak.Writes)
        {
            var now = Snapshot(w);
            if (w.PresenceOnly)
                Check(tweak.Id + ": ключ создан", now.KeyExists);
            else
                Check(tweak.Id + ": в реестре лежит " + w.OnValue,
                    string.Equals(now.Value?.ToString(), w.OnValue.ToString(), StringComparison.Ordinal));
        }

        Check(tweak.Id + ": повторный план пуст",
            RegistryTweakEngine.Plan(tweak, turnOn: true).Count == 0);

        var journal = new ChangeJournal(journalPath);
        journal.Append(tweak, onChanges);
        foreach (var record in journal.Records.Where(r => r.TweakId == tweak.Id && !r.Reverted).ToArray())
            journal.Revert(record);

        var afterRevert = tweak.Writes.Select(Snapshot).ToArray();
        for (var i = 0; i < before.Length; i++)
            Check(tweak.Id + ": откат вернул исходное значение", SameSnap(before[i], afterRevert[i]));

        Check(tweak.Id + ": после отката прежнее состояние",
            RegistryTweakEngine.Read(tweak, build) == startState);

        RegistryTweakEngine.Apply(tweak, turnOn: false);
        Check(tweak.Id + ": после выключения состояние Off",
            RegistryTweakEngine.Read(tweak, build) == TweakState.Off);
    }
    finally
    {
        for (var i = 0; i < tweak.Writes.Count; i++)
            RestoreRaw(tweak.Writes[i], before[i]);
    }

    var restored = tweak.Writes.Select(Snapshot).ToArray();
    for (var i = 0; i < before.Length; i++)
        Check(tweak.Id + ": машина возвращена как была", SameSnap(before[i], restored[i]));

    var endState = RegistryTweakEngine.Read(tweak, build);
    var mark = endState == startState ? "ok" : "РАСХОЖДЕНИЕ";
    Console.WriteLine($"  {mark,-12} {tweak.Id,-28} было {startState}, стало {endState}");
}

if (System.IO.File.Exists(journalPath)) System.IO.File.Delete(journalPath);

Console.WriteLine();
Console.WriteLine("проверок: " + checks + ", провалов: " + failures);
return failures;
