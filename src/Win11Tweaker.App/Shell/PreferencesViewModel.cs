using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace Win11Tweaker.App.Shell;

public sealed class PreferencesStore
{
    readonly string path;
    readonly Dictionary<string, bool> flags;

    public PreferencesStore(string? overridePath = null)
    {
        path = overridePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Win11Tweaker", "preferences.json");

        flags = Load();
    }

    public bool Get(string key, bool fallback)
        => flags.TryGetValue(key, out var saved) ? saved : fallback;

    public void Set(string key, bool value)
    {
        flags[key] = value;
        Save();
    }

    Dictionary<string, bool> Load()
    {
        if (!File.Exists(path))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, bool>>(File.ReadAllText(path)) ?? [];
        }
        catch (Exception)
        {
            return [];
        }
    }

    void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var staging = path + ".tmp";
        File.WriteAllText(staging, JsonSerializer.Serialize(flags, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(staging, path, overwrite: true);
    }
}

public sealed class OptionViewModel : ObservableBase
{
    readonly PreferencesStore store;
    readonly string key;
    bool enabled;

    public OptionViewModel(PreferencesStore store, string key, string title, string summary,
        bool fallback, bool locked, SolidColorBrush accent, SolidColorBrush accentSoft)
    {
        this.store = store;
        this.key = key;
        Title = title;
        Summary = summary;
        Locked = locked;

        enabled = locked ? fallback : store.Get(key, fallback);
        if (locked)
            store.Set(key, fallback);
        Accent = accent;
        AccentSoft = accentSoft;
    }

    public string Title { get; }

    public string Summary { get; }

    public bool Locked { get; }

    public bool Unlocked => !Locked;

    public SolidColorBrush Accent { get; }

    public SolidColorBrush AccentSoft { get; }

    public bool IsEnabled
    {
        get => enabled;
        set
        {
            if (Locked)
                return;
            if (Set(ref enabled, value))
                store.Set(key, value);
        }
    }
}

public sealed class PreferencesViewModel : NavPage
{
    readonly PreferencesStore store;

    public PreferencesViewModel(PreferencesStore store) : base("Настройки", "Прочее", "#71717A")
    {
        this.store = store;
        Options =
        [
            Option("restore-point", "Резервная точка перед изменениями",
                "Ванта сама делает точку восстановления перед пакетом твиков и откатит, если что-то пойдёт не так. "
              + "Ничего настраивать не нужно.", fallback: true, locked: true),
            Option("autostart", "Автозапуск с системой",
                "Держать Win11 Tweaker в фоне и возвращать настройки, если обновление Windows их сбросит.",
                fallback: false, locked: false)
        ];
    }

    public IReadOnlyList<OptionViewModel> Options { get; }

    OptionViewModel Option(string key, string title, string summary, bool fallback, bool locked)
        => new(store, key, title, summary, fallback, locked, Accent, AccentSoft);
}
