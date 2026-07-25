using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;
using Win11Tweaker.App.Interop;

namespace Win11Tweaker.App.Shell;

public interface ILivePage
{
    void Tick();
}

public sealed class ProcessRow(string name, int pid, ProcessMeta meta) : ObservableBase
{
    string memory = string.Empty;
    string cpu = string.Empty;
    bool active;

    public string Name { get; } = name;

    public int Pid { get; } = pid;

    public string PidText { get; } = pid.ToString();

    public string Category => meta.Category;

    public string Publisher => meta.Publisher;

    public bool Killable => meta.Killable;

    public bool Candidate => meta.Candidate && !active;

    public string Memory
    {
        get => memory;
        set => Set(ref memory, value);
    }

    public string Cpu
    {
        get => cpu;
        set => Set(ref cpu, value);
    }

    public bool Active
    {
        get => active;
        set { if (Set(ref active, value)) Raise(nameof(Candidate)); }
    }
}

public sealed class ProcessMonitorViewModel : NavPage, ILivePage
{
    const int Shown = 50;
    const double ActiveThreshold = 1.0;

    readonly TrustList trust;
    readonly Dictionary<int, TimeSpan> lastCpu = [];
    readonly Dictionary<int, ProcessMeta> metaCache = [];
    DateTime lastSample = DateTime.UtcNow;
    bool onlyJunk;

    public ProcessMonitorViewModel(TrustList trust) : base("Процессы", "Система", "#60A5FA")
    {
        this.trust = trust;
        View = CollectionViewSource.GetDefaultView(Rows);
        View.Filter = row => !onlyJunk || ((ProcessRow)row).Candidate;
        ToggleFilter = new RelayCommand(_ =>
        {
            onlyJunk = !onlyJunk;
            View.Refresh();
            Raise(nameof(FilterLabel));
        });
        Tick();
    }

    public ObservableCollection<ProcessRow> Rows { get; } = [];

    public ICollectionView View { get; }

    public RelayCommand ToggleFilter { get; }

    public string FilterLabel => onlyJunk ? "Показать все" : "Только лишние";

    public int JunkCount => Rows.Count(r => r.Candidate);

    public string Summary
    {
        get
        {
            var important = Rows.Count(r => r.Category is "Система" or "Windows" or "Доверенный");
            var apps = Rows.Count(r => r.Category == "Приложение");
            return "важных " + important + "  ·  приложений " + apps + "  ·  лишних " + JunkCount;
        }
    }

    public override string Badge => JunkCount == 0 ? string.Empty : JunkCount.ToString();

    public bool Terminate(ProcessRow row)
    {
        if (!row.Killable)
            return false;

        try
        {
            using var proc = Process.GetProcessById(row.Pid);
            proc.Kill();
            Rows.Remove(row);
            Raise(nameof(Summary));
            Raise(nameof(JunkCount));
            Raise(nameof(Badge));
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public void Tick()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - lastSample).TotalSeconds;
        lastSample = now;

        var live = Sample(elapsed);

        for (var slot = 0; slot < live.Count; slot++)
        {
            var fresh = live[slot];
            var existing = Rows.FirstOrDefault(r => r.Pid == fresh.Pid);

            if (existing is null)
            {
                Rows.Insert(Math.Min(slot, Rows.Count), fresh);
                continue;
            }

            existing.Memory = fresh.Memory;
            existing.Cpu = fresh.Cpu;
            existing.Active = fresh.Active;

            var at = Rows.IndexOf(existing);
            if (at != slot)
                Rows.Move(at, slot);
        }

        var alive = live.Select(r => r.Pid).ToHashSet();
        for (var slot = Rows.Count - 1; slot >= 0; slot--)
            if (!alive.Contains(Rows[slot].Pid))
                Rows.RemoveAt(slot);

        Raise(nameof(Badge));
        Raise(nameof(JunkCount));
        Raise(nameof(Summary));
    }

    List<ProcessRow> Sample(double elapsedSeconds)
    {
        var cores = Environment.ProcessorCount;
        var seen = new HashSet<int>();
        var sampled = new List<ProcessRow>();

        foreach (var proc in Process.GetProcesses().OrderByDescending(p => p.WorkingSet64).Take(Shown))
        {
            if (!metaCache.TryGetValue(proc.Id, out var meta))
            {
                meta = ProcessClassifier.Classify(proc, trust);
                metaCache[proc.Id] = meta;
            }

            var row = new ProcessRow(proc.ProcessName, proc.Id, meta)
            {
                Memory = (proc.WorkingSet64 / 1024d / 1024d).ToString("0") + " МБ",
                Cpu = "0%"
            };

            seen.Add(proc.Id);

            try
            {
                var cpuNow = proc.TotalProcessorTime;

                if (elapsedSeconds > 0 && lastCpu.TryGetValue(proc.Id, out var cpuThen))
                {
                    var percent = (cpuNow - cpuThen).TotalSeconds / (elapsedSeconds * cores) * 100;
                    row.Cpu = Math.Clamp(percent, 0, 100).ToString("0.#") + "%";
                    row.Active = percent >= ActiveThreshold;
                }

                lastCpu[proc.Id] = cpuNow;
            }
            catch (Exception)
            {
            }

            sampled.Add(row);
            proc.Dispose();
        }

        foreach (var stale in lastCpu.Keys.Where(pid => !seen.Contains(pid)).ToArray())
            lastCpu.Remove(stale);
        foreach (var stale in metaCache.Keys.Where(pid => !seen.Contains(pid)).ToArray())
            metaCache.Remove(stale);

        return sampled;
    }
}

public sealed class MemoryHog(string name, int pid) : ObservableBase
{
    string working = string.Empty;
    double share;

    public string Name { get; } = name;

    public int Pid { get; } = pid;

    public string PidText { get; } = pid.ToString();

    public string Working
    {
        get => working;
        set => Set(ref working, value);
    }

    public double Share
    {
        get => share;
        set => Set(ref share, value);
    }
}

public sealed class MemoryViewModel : NavPage, ILivePage
{
    public MemoryViewModel() : base("Память", "Система", "#C084FC")
    {
        Tick();
    }

    public string TotalLabel { get; private set; } = string.Empty;

    public string UsedLabel { get; private set; } = string.Empty;

    public string FreeLabel { get; private set; } = string.Empty;

    public uint LoadPercent { get; private set; }

    public ObservableCollection<MemoryHog> Hogs { get; } = [];

    public override string Badge => LoadPercent + "%";

    public void Tick()
    {
        var (total, used, load) = MachineFacts.MemorySnapshot();
        TotalLabel = total.ToString("0.#") + " ГБ";
        UsedLabel = used.ToString("0.#") + " ГБ";
        FreeLabel = (total - used).ToString("0.#") + " ГБ";
        LoadPercent = load;

        var totalBytes = total * 1024 * 1024 * 1024;
        var slot = 0;

        foreach (var proc in Process.GetProcesses().OrderByDescending(p => p.WorkingSet64).Take(14))
        {
            var share = totalBytes > 0 ? proc.WorkingSet64 / totalBytes : 0;
            var text = (proc.WorkingSet64 / 1024d / 1024d).ToString("0") + " МБ";

            if (slot < Hogs.Count && Hogs[slot].Pid == proc.Id)
            {
                Hogs[slot].Working = text;
                Hogs[slot].Share = share;
            }
            else
            {
                var row = new MemoryHog(proc.ProcessName, proc.Id) { Working = text, Share = share };
                if (slot < Hogs.Count) Hogs[slot] = row; else Hogs.Add(row);
            }

            slot++;
            proc.Dispose();
        }

        while (Hogs.Count > slot)
            Hogs.RemoveAt(Hogs.Count - 1);

        Raise(nameof(TotalLabel));
        Raise(nameof(UsedLabel));
        Raise(nameof(FreeLabel));
        Raise(nameof(LoadPercent));
        Raise(nameof(Badge));
    }
}
