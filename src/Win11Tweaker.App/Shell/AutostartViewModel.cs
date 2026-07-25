using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Win11Tweaker.App.Interop;

namespace Win11Tweaker.App.Shell;

public sealed class AutostartRow : ObservableBase
{
    readonly AutostartEntry entry;
    readonly Action<AutostartRow, bool> onToggle;
    bool enabled;

    public AutostartRow(AutostartEntry entry, Action<AutostartRow, bool> onToggle)
    {
        this.entry = entry;
        this.onToggle = onToggle;
        enabled = entry.Enabled;
    }

    public AutostartEntry Entry => entry;

    public string Name => entry.Name;

    public string Publisher => entry.Publisher;

    public bool Trusted => entry.Trusted;

    public string SourceLabel => entry.Source switch
    {
        AutoSource.CurrentUserRun => "реестр · вы",
        AutoSource.LocalMachineRun => "реестр · система",
        AutoSource.LocalMachineRun32 => "реестр · система",
        AutoSource.ScheduledTask => "планировщик",
        _ => "папка автозагрузки"
    };

    public string StatusLabel => entry.Trusted ? "доверенный" : "неизвестный";

    public bool IsEnabled
    {
        get => enabled;
        set { if (Set(ref enabled, value)) onToggle(this, value); }
    }
}

public sealed class AutostartViewModel : NavPage, IActivate
{
    readonly TrustList trust;
    readonly PreferencesStore store;
    bool autoDisable;

    public AutostartViewModel(TrustList trust, PreferencesStore store)
        : base("Автозагрузка", "Обслуживание", "#34D399")
    {
        this.trust = trust;
        this.store = store;
        autoDisable = store.Get("autostart.auto", false);

        Load();

        if (autoDisable)
            DisableUnknown();
    }

    public ObservableCollection<AutostartRow> Rows { get; } = [];

    public int UnknownCount => Rows.Count(r => !r.Trusted);

    public string Summary => "всего " + Rows.Count + "  ·  неизвестных " + UnknownCount;

    public bool AutoDisable
    {
        get => autoDisable;
        set
        {
            if (!Set(ref autoDisable, value))
                return;

            store.Set("autostart.auto", value);
            if (value)
                DisableUnknown();
        }
    }

    public void Activate() => Load();

    public void Load()
    {
        Rows.Clear();
        foreach (var entry in Autostart.Scan(trust).OrderBy(e => e.Trusted).ThenBy(e => e.Name))
            Rows.Add(new AutostartRow(entry, Toggle));

        Raise(nameof(UnknownCount));
        Raise(nameof(Summary));
    }

    public void AddToTrusted(AutostartRow row)
    {
        trust.Add(row.Name);
        Load();
    }

    void Toggle(AutostartRow row, bool enabled)
    {
        try
        {
            Autostart.SetEnabled(row.Entry, enabled);
        }
        catch (Exception)
        {
        }
    }

    void DisableUnknown()
    {
        foreach (var row in Rows.Where(r => !r.Trusted && r.IsEnabled).ToArray())
            row.IsEnabled = false;

        Raise(nameof(Summary));
    }
}
