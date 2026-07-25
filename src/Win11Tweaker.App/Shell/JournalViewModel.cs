using System.Collections.Generic;
using System.Linq;
using Win11Tweaker.App.Interop;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Shell;

public sealed class JournalRow(JournalRecord record, Action afterRevert) : ObservableBase
{
    public string Stamp => record.At.ToString("dd.MM HH:mm");

    public string Title => record.TweakTitle;

    public string Target => record.Target;

    public string Before => record.Before ?? "(не задано)";

    public string After => record.After ?? "(не задано)";

    public bool Reverted => record.Reverted;

    public JournalRecord Record => record;

    internal void Refresh()
    {
        Raise(nameof(Reverted));
        afterRevert();
    }
}

public sealed class JournalViewModel : NavPage
{
    readonly ChangeJournal journal;
    List<JournalRow> rows = [];

    public JournalViewModel(ChangeJournal journal) : base("Журнал", "Прочее", "#8B96A8")
    {
        this.journal = journal;
        Revert = new RelayCommand(RevertOne, target => target is JournalRow { Reverted: false });
        Reload();
    }

    public RelayCommand Revert { get; }

    public IReadOnlyList<JournalRow> Rows => rows;

    public bool IsEmpty => rows.Count == 0;

    public override string Badge
    {
        get
        {
            var live = rows.Count(r => !r.Reverted);
            return live == 0 ? string.Empty : live.ToString();
        }
    }

    public event Action? Reverted;

    public void Reload()
    {
        rows = journal.Recent().Select(r => new JournalRow(r, () => Raise(nameof(Badge)))).ToList();
        Raise(nameof(Rows));
        Raise(nameof(IsEmpty));
        Raise(nameof(Badge));
    }

    void RevertOne(object? target)
    {
        if (target is not JournalRow row || row.Reverted)
            return;

        journal.Revert(row.Record);
        row.Refresh();
        ShellRefresh.Nudge();
        Raise(nameof(Badge));
        Reverted?.Invoke();
    }
}
