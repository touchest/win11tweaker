using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using Win11Tweaker.App.Catalog;
using Win11Tweaker.App.Interop;

namespace Win11Tweaker.App.Shell;

public sealed class TaskRowViewModel : ObservableBase
{
    NavPage? owner;
    bool disabledNow;
    bool wanted;

    public TaskRowViewModel(TaskItemSpec spec)
    {
        Spec = spec;
        disabledNow = AllDisabled();
        wanted = disabledNow;
    }

    public TaskItemSpec Spec { get; }

    internal void Adopt(NavPage page) => owner ??= page;

    public string Title => Spec.Title;

    public string Summary => Spec.Summary;

    public bool IsAvailable => Spec.Paths.Any(TaskManager.Exists);

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

    public bool IsPending => IsAvailable && wanted != disabledNow;

    internal void Apply()
    {
        foreach (var path in Spec.Paths.Where(TaskManager.Exists).ToArray())
            TaskManager.SetDisabled(path, wanted);
    }

    internal void Resync()
    {
        disabledNow = AllDisabled();
        wanted = disabledNow;
        Raise(nameof(IsEnabled));
        Raise(nameof(IsPending));
        Raise(nameof(IsAvailable));
    }

    bool AllDisabled()
    {
        var existing = Spec.Paths.Where(TaskManager.Exists).ToArray();
        return existing.Length > 0 && existing.All(TaskManager.IsDisabled);
    }
}

public sealed record TaskGroup(string Title, IReadOnlyList<TaskRowViewModel> Rows);

public sealed class TaskSectionViewModel : NavPage
{
    public TaskSectionViewModel(string accentHex, IReadOnlyList<TaskGroupSpec> groups)
        : base("Задачи", "Твики", accentHex)
    {
        Groups = groups
            .Select(g => new TaskGroup(
                g.Title,
                g.Items.Where(i => i.Paths.Any(TaskManager.Exists))
                       .Select(i => new TaskRowViewModel(i))
                       .ToArray()))
            .Where(g => g.Rows.Count > 0)
            .ToArray();

        Rows = Groups.SelectMany(g => g.Rows).ToArray();

        foreach (var row in Rows)
        {
            row.Adopt(this);
            row.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName is nameof(TaskRowViewModel.IsEnabled) or nameof(TaskRowViewModel.IsPending))
                    RaisePending();
            };
        }
    }

    public string Subtitle => "Фоновые задания планировщика. Отметьте наборы для отключения и нажмите «Применить».";

    public IReadOnlyList<TaskGroup> Groups { get; }

    public IReadOnlyList<TaskRowViewModel> Rows { get; }

    public bool HasPending => Rows.Any(r => r.IsPending);

    public int PendingCount => Rows.Count(r => r.IsPending);

    public bool IsBusy { get; private set; }

    public bool CanApply => HasPending && !IsBusy;

    public string PendingLabel => IsBusy ? "Применение…" : PendingCount == 0 ? "Нет изменений" : "Применить (" + PendingCount + ")";

    public string StatusText { get; private set; } = string.Empty;

    public bool ShowStatus => StatusText.Length > 0;

    public RelayCommand DismissStatus => new(_ =>
    {
        StatusText = string.Empty;
        Raise(nameof(StatusText));
        Raise(nameof(ShowStatus));
    });

    public async System.Threading.Tasks.Task ApplyAsync()
    {
        var targets = Rows.Where(r => r.IsPending).ToArray();
        if (targets.Length == 0 || IsBusy)
            return;

        IsBusy = true;
        StatusText = "Применение…";
        Raise(nameof(IsBusy));
        Raise(nameof(StatusText));
        Raise(nameof(ShowStatus));
        RaisePending();

        await System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var row in targets)
                row.Apply();
        });

        foreach (var row in Rows)
            row.Resync();

        IsBusy = false;
        StatusText = "Готово. Изменено наборов: " + targets.Length + ".";
        Raise(nameof(IsBusy));
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
