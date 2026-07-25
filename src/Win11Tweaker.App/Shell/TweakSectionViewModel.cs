using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Win11Tweaker.App.Catalog;
using Win11Tweaker.App.Interop;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Shell;

public sealed class TweakViewModel : ObservableBase
{
    static readonly SolidColorBrush SafeInk = Frozen("#FF4ADE80");
    static readonly SolidColorBrush ModerateInk = Frozen("#FFFBBF24");
    static readonly SolidColorBrush RiskyInk = Frozen("#FFF87171");

    NavPage? owner;
    TweakState observed;
    bool wanted;

    public TweakViewModel(TweakDefinition spec, TweakState observed)
    {
        Spec = spec;
        this.observed = observed;
        wanted = observed == TweakState.On;
        DependencyWarning = BuildDependencyWarning(spec);
    }

    public TweakDefinition Spec { get; }

    public string DependencyWarning { get; }

    public bool HasDependencyWarning => DependencyWarning.Length > 0;

    static string BuildDependencyWarning(TweakDefinition spec)
    {
        var services = spec.Writes
            .Where(w => w.Root == RegistryRoot.LocalMachine && w.Name == "Start"
                        && w.Key.Contains(@"\Services\") && Equals(w.OnValue, 4))
            .Select(w => w.Key[(w.Key.LastIndexOf('\\') + 1)..])
            .ToArray();

        if (services.Length == 0)
            return string.Empty;

        var dependents = services
            .SelectMany(Interop.ServiceDependencies.Dependents)
            .Distinct()
            .ToArray();

        return dependents.Length == 0
            ? string.Empty
            : "Зависят от этой службы: " + string.Join(", ", dependents) + ". Они тоже перестанут работать.";
    }

    internal void Adopt(NavPage page) => owner ??= page;

    public string Title => Spec.Title;

    public string Summary => Spec.Summary;

    public bool IsAvailable => observed != TweakState.Unsupported;

    public SolidColorBrush Accent => owner!.Accent;

    public SolidColorBrush AccentSoft => owner!.AccentSoft;

    public bool IsEnabled
    {
        get => wanted;
        set
        {
            if (Set(ref wanted, value))
                Raise(nameof(IsPending));
        }
    }

    public bool IsPending => IsAvailable && wanted != (observed == TweakState.On);

    public TweakState Observed => observed;

    public SolidColorBrush RiskInk => Spec.Risk switch
    {
        RiskLevel.Safe => SafeInk,
        RiskLevel.Moderate => ModerateInk,
        _ => RiskyInk
    };

    public string RiskLabel => Spec.Risk switch
    {
        RiskLevel.Safe => "безопасно",
        RiskLevel.Moderate => "средний риск",
        _ => "рискованно"
    };

    public string Note => string.Empty;

    public bool HasNote => false;

    public string Target => string.Join("\n", Spec.Writes.Select(w => w.Display));

    internal void Resync(int build)
    {
        observed = RegistryTweakEngine.Read(Spec, build);
        wanted = observed == TweakState.On;
        Raise(nameof(IsEnabled));
        Raise(nameof(IsPending));
        Raise(nameof(Note));
        Raise(nameof(HasNote));
        Raise(nameof(IsAvailable));
    }

    static SolidColorBrush Frozen(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
        brush.Freeze();
        return brush;
    }
}

public sealed record TweakGroup(string Title, IReadOnlyList<TweakViewModel> Tweaks);

public sealed class TweakSectionViewModel : NavPage
{
    readonly ChangeJournal journal;
    readonly int build;

    readonly Func<string?>? gate;

    public TweakSectionViewModel(
        string title,
        string accentHex,
        string subtitle,
        IReadOnlyList<TweakGroupSpec> specs,
        ChangeJournal journal,
        int build,
        string group = "Твики",
        bool requiresAdmin = false,
        Func<string?>? gate = null)
        : base(title, group, accentHex)
    {
        this.journal = journal;
        this.build = build;
        this.gate = gate;
        RequiresAdmin = requiresAdmin;
        Subtitle = subtitle;

        Groups = specs
            .Select(g => new TweakGroup(
                g.Title,
                g.Tweaks
                    .Select(t => new TweakViewModel(t, RegistryTweakEngine.Read(t, build)))
                    .Where(t => t.IsAvailable)
                    .ToArray()))
            .Where(g => g.Tweaks.Count > 0)
            .ToArray();

        Tweaks = Groups.SelectMany(g => g.Tweaks).ToArray();

        foreach (var tweak in Tweaks)
        {
            tweak.Adopt(this);
            tweak.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(TweakViewModel.IsEnabled) or nameof(TweakViewModel.IsPending))
                    RaisePending();
            };
        }
    }

    public string Subtitle { get; }

    public bool RequiresAdmin { get; }

    public string? Blocked => gate?.Invoke();

    public bool IsBlocked => Blocked is not null;

    public bool NeedsElevation => RequiresAdmin && !Interop.Elevation.IsAdmin;

    public void RefreshGate()
    {
        Raise(nameof(Blocked));
        Raise(nameof(IsBlocked));
    }

    public IReadOnlyList<TweakGroup> Groups { get; }

    public IReadOnlyList<TweakViewModel> Tweaks { get; }

    public IReadOnlyList<TweakViewModel> Pending => Tweaks.Where(t => t.IsPending).ToArray();

    public bool HasPending => Tweaks.Any(t => t.IsPending);

    public string PendingLabel
    {
        get
        {
            var count = Pending.Count;
            if (count == 0)
                return "Нет изменений";

            var prefix = NeedsElevation ? "Применить от админа (" : "Применить (";
            return prefix + count + ")";
        }
    }

    public override string Badge
    {
        get
        {
            var live = Tweaks.Count(t => t.Observed == TweakState.On);
            return live == 0 ? string.Empty : live.ToString();
        }
    }

    public IReadOnlyList<(TweakViewModel Tweak, Change Change)> BuildPlan()
    {
        var plan = new List<(TweakViewModel, Change)>();

        foreach (var tweak in Pending)
            foreach (var change in RegistryTweakEngine.Plan(tweak.Spec, tweak.IsEnabled))
                plan.Add((tweak, change));

        return plan;
    }

    public string StatusText { get; private set; } = string.Empty;

    public bool ShowStatus => StatusText.Length > 0;

    public bool CanRestartShell { get; private set; }

    public RelayCommand RestartShell => new(_ =>
    {
        ShellRefresh.RestartExplorer();
        CanRestartShell = false;
        StatusText = "Проводник перезапущен.";
        Raise(nameof(CanRestartShell));
        Raise(nameof(StatusText));
    });

    public RelayCommand DismissStatus => new(_ =>
    {
        StatusText = string.Empty;
        CanRestartShell = false;
        Raise(nameof(StatusText));
        Raise(nameof(ShowStatus));
        Raise(nameof(CanRestartShell));
    });

    public ApplyOutcome Apply()
    {
        var touched = 0;
        var needsRestart = false;
        var needsReboot = false;

        var pending = Pending;
        var results = new List<(TweakDefinition Spec, IReadOnlyList<Change> Changes)>();
        var failed = new List<string>();

        var denied = new List<TweakViewModel>();
        foreach (var tweak in pending)
        {
            try
            {
                results.Add((tweak.Spec, RegistryTweakEngine.Apply(tweak.Spec, tweak.IsEnabled)));
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException)
            {
                denied.Add(tweak);
            }
        }

        var viaTi = denied.Where(t => t.Spec.Writes.All(w => w.Root == RegistryRoot.LocalMachine)).ToArray();
        failed.AddRange(denied.Except(viaTi).Select(t => t.Spec.Title));

        if (viaTi.Length > 0)
        {
            try
            {
                Interop.TrustedInstaller.RunAs(() =>
                {
                    foreach (var tweak in viaTi)
                    {
                        try { results.Add((tweak.Spec, RegistryTweakEngine.Apply(tweak.Spec, tweak.IsEnabled))); }
                        catch (Exception) { failed.Add(tweak.Spec.Title); }
                    }
                });
            }
            catch (Exception)
            {
                failed.AddRange(viaTi.Select(t => t.Spec.Title));
            }
        }

        foreach (var (spec, changes) in results)
        {
            if (changes.Count == 0)
                continue;

            journal.Append(spec, changes);
            touched += changes.Count;
            needsRestart |= spec.NeedsShellRestart;
            needsReboot |= spec.NeedsSignOut;
        }

        foreach (var tweak in Tweaks)
            tweak.Resync(build);

        RaisePending();
        Raise(nameof(Badge));

        ShellRefresh.Nudge();

        var tail = needsReboot
            ? " ⚠ Чтобы изменения вступили в силу, перезагрузите компьютер вручную."
            : needsRestart ? " Часть изменений покажется после перезапуска проводника." : string.Empty;

        var head = touched == 0
            ? "Нечего было записывать: значения уже стояли как надо."
            : "Записано значений: " + touched + "." + tail;

        StatusText = failed.Count == 0
            ? head
            : head + " Не удалось (нужны особые права): " + string.Join(", ", failed) + ".";

        CanRestartShell = needsRestart;
        Raise(nameof(StatusText));
        Raise(nameof(ShowStatus));
        Raise(nameof(CanRestartShell));

        return new ApplyOutcome(touched, needsRestart);
    }

    public void ResyncAll()
    {
        foreach (var tweak in Tweaks)
            tweak.Resync(build);

        RaisePending();
        Raise(nameof(Badge));
    }

    void RaisePending()
    {
        Raise(nameof(HasPending));
        Raise(nameof(PendingLabel));
    }
}

public sealed record ApplyOutcome(int Written, bool NeedsShellRestart);
