using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;
using Win11Tweaker.App.Catalog;
using Win11Tweaker.App.Interop;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Shell;

public sealed class ShellViewModel : ObservableBase
{
    readonly TweakSectionViewModel[] sections;
    NavPage selected;
    string query = string.Empty;
    TweakSectionViewModel? hits;

    public ShellViewModel()
    {
        Interop.SafeModeDefenderKill.CleanupResidue();

        Journal = new ChangeJournal();
        Preferences = new PreferencesStore();
        Trust = new TrustList();
        var build = MachineFacts.BuildNumber();

        ContextMenu = new TweakSectionViewModel(
            "Контекстное меню", "#A78BFA",
            "Классическое меню Windows 10, скорость раскрытия и полезные пункты правой кнопки.",
            ContextMenuTweaks.Build(), Journal, build,
            requiresAdmin: true);

        Interface = new TweakSectionViewModel(
            "Интерфейс", "#A78BFA",
            "Проводник, панель задач и оформление.",
            InterfaceTweaks.Build(), Journal, build,
            requiresAdmin: true);

        SystemPage = new TweakSectionViewModel(
            "Система и службы", "#2DD4BF",
            "Приватность, быстродействие, лишние функции и службы. Выключайте то, чем не пользуетесь.",
            [.. PrivacyTweaks.Build(), .. PerformanceTweaks.Build(), .. ExtrasTweaks.Build(), .. ServiceTweaks.Build()],
            Journal, build,
            requiresAdmin: true);

        Services = new ServiceQuizViewModel();

        Apps = new AppxSectionViewModel("#E879F9", AppxCatalog.Build());

        sections = [ContextMenu, Interface, SystemPage];

        Overview = new OverviewViewModel(this, Journal);

        Master = new MasterQuizViewModel(this);
        Cleanup = new CleanupViewModel(Preferences);
        AutostartPage = new AutostartViewModel(Trust, Preferences);
        Optimize = new OptimizeViewModel();
        Settings = new PreferencesViewModel(Preferences);

        foreach (var section in sections)
            HookStatus(section);

        Apps.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(AppxSectionViewModel.StatusText) && Apps.StatusText.Length > 0)
                Notify(Apps.StatusText);
        };

        Pages =
        [
            Overview,
            Master,
            ContextMenu,
            Interface,
            SystemPage,
            Cleanup,
            Apps,
            AutostartPage
        ];

        selected = Overview;

        NavView = CollectionViewSource.GetDefaultView(Pages);

        heartbeat = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        heartbeat.Tick += (_, _) => (Current as ILivePage)?.Tick();
        heartbeat.Start();
    }

    readonly DispatcherTimer heartbeat;

    public BusyService Busy => BusyService.Instance;

    public ChangeJournal Journal { get; }

    public PreferencesStore Preferences { get; }

    public TrustList Trust { get; }

    public CleanupViewModel Cleanup { get; }

    public AutostartViewModel AutostartPage { get; }

    public OptimizeViewModel Optimize { get; }

    public PreferencesViewModel Settings { get; }

    public IReadOnlyList<TweakSectionViewModel> TweakSections => sections;

    public TweakSectionViewModel ContextMenu { get; }

    public TweakSectionViewModel Interface { get; }

    public TweakSectionViewModel SystemPage { get; }

    public ServiceQuizViewModel Services { get; }

    public AppxSectionViewModel Apps { get; }

    public OverviewViewModel Overview { get; }

    public MasterQuizViewModel Master { get; }

    public IReadOnlyList<NavPage> Pages { get; }

    public ICollectionView NavView { get; }

    public NavPage Selected
    {
        get => selected;
        set
        {
            if (!Set(ref selected, value))
                return;

            if (query.Length > 0)
            {
                query = string.Empty;
                hits = null;
                Raise(nameof(Query));
            }

            (value as CategoryViewModel)?.Enter();

            Raise(nameof(Current));
        }
    }

    public string Query
    {
        get => query;
        set
        {
            if (!Set(ref query, value))
                return;

            hits = query.Trim().Length > 0 ? Collect(query.Trim()) : null;
            Raise(nameof(Current));
        }
    }

    public NavPage Current => hits ?? selected;

    public void AfterApply()
    {
        Overview.Recount();
    }

    public ObservableCollection<ToastViewModel> Toasts { get; } = [];

    public void Notify(string text, string? actionLabel = null, Action? action = null)
    {
        Toasts.Add(new ToastViewModel(text, actionLabel, action, t => Toasts.Remove(t)));

        while (Toasts.Count > 3)
            Toasts.RemoveAt(0);
    }

    void HookStatus(TweakSectionViewModel section)
        => section.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(TweakSectionViewModel.StatusText)
                || section.StatusText.Length == 0)
                return;

            var canRestart = section.CanRestartShell;
            Notify(section.StatusText,
                   canRestart ? "Перезапустить проводник" : null,
                   canRestart ? () => section.RestartShell.Execute(null) : null);
        };

    TweakSectionViewModel Collect(string needle)
    {
        var groups = new List<TweakGroupSpec>();
        var found = 0;

        foreach (var section in sections)
        {
            var matched = section.Tweaks
                .Where(t => Contains(t.Title, needle) || Contains(t.Summary, needle) || Contains(t.Spec.Id, needle))
                .Select(t => t.Spec)
                .ToArray();

            if (matched.Length == 0)
                continue;

            found += matched.Length;
            groups.Add(new TweakGroupSpec(section.Title, matched));
        }

        var subtitle = found == 0
            ? "Ничего не нашлось по запросу «" + needle + "»."
            : "Совпадений: " + found + ".";

        return new TweakSectionViewModel("Найдено", "#E8E8EC", subtitle, groups, Journal, MachineFacts.BuildNumber());
    }

    static bool Contains(string haystack, string needle)
        => haystack.Contains(needle, StringComparison.CurrentCultureIgnoreCase);
}
