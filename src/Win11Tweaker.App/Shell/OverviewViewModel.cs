using System.Collections.Generic;
using System.IO;
using System.Linq;
using Win11Tweaker.App.Interop;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Shell;

public sealed record Stat(string Caption, string Value, string Hint);

public sealed record Fact(string Caption, string Value);

public sealed record Volume(
    string Label, string Format, string Used, string Total, string Free, string Percent, double Share, bool Tight);

public sealed record Entry(string Stamp, string Title, string Target, bool Reverted);

public sealed class OverviewViewModel : NavPage
{
    public OverviewViewModel(ShellViewModel shell, ChangeJournal journal)
        : base("Профиль", "Система", "#22D3EE")
    {
        Shell = shell;

        Facts =
        [
            new Fact("Система", MachineFacts.Edition()),
            new Fact("Сборка", MachineFacts.BuildLabel()),
            new Fact("Процессор", MachineFacts.Processor()),
            new Fact("Видеокарта", MachineFacts.Graphics()),
            new Fact("Память", MachineFacts.Memory()),
            new Fact("Компьютер", Environment.MachineName + "  /  " + Environment.UserName)
        ];

        foreach (var s in shell.TweakSections)
            s.PropertyChanged += (_, _) => Raise(nameof(Stats));

        shell.Services.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ServiceQuizViewModel.DisabledCount))
                Raise(nameof(Stats));
        };

        Journal = journal;
    }

    ChangeJournal Journal { get; }

    public IReadOnlyList<Entry> History => Journal.Recent(12)
        .Select(r => new Entry(r.At.ToString("dd.MM  HH:mm"), r.TweakTitle, r.Target, r.Reverted))
        .ToArray();

    public bool HistoryEmpty => !Journal.Recent(1).Any();

    public ShellViewModel Shell { get; }

    public IReadOnlyList<Fact> Facts { get; }

    public IReadOnlyList<Volume> Volumes => ReadVolumes();

    static IReadOnlyList<Volume> ReadVolumes()
    {
        var drives = new List<Volume>();

        foreach (var drive in DriveInfo.GetDrives())
        {
            if (!drive.IsReady || drive.TotalSize == 0)
                continue;

            const double gb = 1024d * 1024d * 1024d;
            var total = drive.TotalSize / gb;
            var free = drive.TotalFreeSpace / gb;
            var used = total - free;
            var share = used / total;

            drives.Add(new Volume(
                drive.Name.TrimEnd('\\') + (drive.VolumeLabel.Length > 0 ? "  " + drive.VolumeLabel : string.Empty),
                drive.DriveFormat,
                used.ToString("0.#") + " ГБ",
                total.ToString("0.#") + " ГБ",
                free.ToString("0.#") + " ГБ",
                (share * 100).ToString("0") + "%",
                share,
                share > 0.9));
        }

        return drives;
    }

    public IReadOnlyList<Stat> Stats
    {
        get
        {
            var all = Shell.TweakSections.SelectMany(s => s.Tweaks).ToArray();
            var total = all.Length;
            var applied = all.Count(t => t.Observed == TweakState.On);
            var servicesOff = Shell.Services.DisabledCount;

            return
            [
                new Stat("Твиков применено", applied.ToString(), "из " + total + " возможных"),
                new Stat("Служб выключено", servicesOff.ToString(), "фоновых служб отключено"),
                new Stat("Свободно на C:", SystemDriveFree(), "на системном диске")
            ];
        }
    }

    static string SystemDriveFree()
    {
        try
        {
            var root = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows)) ?? "C:\\";
            var drive = new DriveInfo(root);
            const double gb = 1024d * 1024d * 1024d;
            return (drive.TotalFreeSpace / gb).ToString("0") + " ГБ";
        }
        catch (Exception) { return "?"; }
    }

    public void Recount()
    {
        Raise(nameof(Stats));
        Raise(nameof(History));
        Raise(nameof(HistoryEmpty));
    }
}
