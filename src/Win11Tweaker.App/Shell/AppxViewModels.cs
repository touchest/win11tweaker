using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Win11Tweaker.App.Catalog;
using Win11Tweaker.App.Interop;

namespace Win11Tweaker.App.Shell;

public sealed class AppxRowViewModel : ObservableBase
{
    NavPage? owner;
    bool installed;
    bool wanted;

    public AppxRowViewModel(AppxAppSpec spec)
    {
        Spec = spec;
        installed = spec.Packages.Any(AppxManager.IsInstalled);
        wanted = !installed;
    }

    public AppxAppSpec Spec { get; }

    internal void Adopt(NavPage page) => owner ??= page;

    public string Title => Spec.Title;

    public string Summary => Spec.Summary;

    public bool Installed => installed;

    public bool IsAvailable => installed;

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

    public bool IsPending => installed && wanted;

    public string Note => installed ? string.Empty : "удалено";

    public bool HasNote => !installed;

    internal void Resync()
    {
        installed = Spec.Packages.Any(AppxManager.IsInstalled);
        wanted = !installed;
        Raise(nameof(IsEnabled));
        Raise(nameof(IsPending));
        Raise(nameof(IsAvailable));
        Raise(nameof(Installed));
        Raise(nameof(Note));
        Raise(nameof(HasNote));
    }
}

public sealed record AppxGroup(string Title, IReadOnlyList<AppxRowViewModel> Rows);

public sealed class AppxSectionViewModel : NavPage
{
    public AppxSectionViewModel(string accentHex, IReadOnlyList<AppxGroupSpec> groups)
        : base("Приложения", "Твики", accentHex)
    {
        Groups = groups
            .Select(g => new AppxGroup(
                g.Title,
                g.Apps.Where(a => a.Packages.Any(AppxManager.IsInstalled))
                      .Select(a => new AppxRowViewModel(a))
                      .ToArray()))
            .Where(g => g.Rows.Count > 0)
            .ToArray();

        Rows = Groups.SelectMany(g => g.Rows).ToArray();

        foreach (var row in Rows)
        {
            row.Adopt(this);
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(AppxRowViewModel.IsEnabled) or nameof(AppxRowViewModel.IsPending))
                    RaisePending();
            };
        }
    }

    public string Subtitle => "Удаление встроенных приложений. Отметь ненужные и нажми «Удалить». Показаны только те, что стоят на этой машине.";

    public IReadOnlyList<AppxGroup> Groups { get; }

    public IReadOnlyList<AppxRowViewModel> Rows { get; }

    public bool HasPending => Rows.Any(r => r.IsPending);

    public int PendingCount => Rows.Count(r => r.IsPending);

    public bool CanApply => HasPending && !IsBusy;

    public string PendingLabel => IsBusy ? "Удаление…" : PendingCount == 0 ? "Нет изменений" : "Удалить (" + PendingCount + ")";

    public string StatusText { get; private set; } = string.Empty;

    public bool ShowStatus => StatusText.Length > 0;

    public RelayCommand DismissStatus => new(_ =>
    {
        StatusText = string.Empty;
        Raise(nameof(StatusText));
        Raise(nameof(ShowStatus));
    });

    public override string Badge
    {
        get
        {
            var gone = Rows.Count(r => !r.Installed);
            return gone == 0 ? string.Empty : gone.ToString();
        }
    }

    public bool IsBusy { get; private set; }

    public async System.Threading.Tasks.Task ApplyAsync()
    {
        var targets = Rows.Where(r => r.IsPending).ToArray();
        if (targets.Length == 0 || IsBusy)
            return;

        IsBusy = true;
        StatusText = "Удаление… это может занять минуту.";
        Raise(nameof(IsBusy));
        Raise(nameof(StatusText));
        Raise(nameof(ShowStatus));
        RaisePending();

        await System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var row in targets)
            {
                foreach (var package in row.Spec.Packages)
                    AppxManager.Remove(package);

                if (row.Spec.UnpinMatch is { } match)
                    AppxManager.UnpinFromTaskbar(match);
            }
        });

        foreach (var row in Rows)
            row.Resync();

        ShellRefresh.Nudge();

        IsBusy = false;
        StatusText = "Удалено приложений: " + targets.Length
                   + ". Часть значков обновится после перезапуска проводника.";
        Raise(nameof(IsBusy));
        Raise(nameof(Badge));
        Raise(nameof(StatusText));
        Raise(nameof(ShowStatus));
        RaisePending();
    }

    void RaisePending()
    {
        Raise(nameof(HasPending));
        Raise(nameof(PendingCount));
        Raise(nameof(PendingLabel));
        Raise(nameof(CanApply));
    }
}
