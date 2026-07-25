using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Win11Tweaker.App.Interop;

namespace Win11Tweaker.App.Shell;

public sealed class CleanupItem : ObservableBase
{
    readonly Func<long>? measure;
    readonly Func<SweepResult> purge;
    readonly PreferencesStore store;
    readonly string key;
    long rawBytes;
    SweepResult rawResult;
    bool @checked;
    string sizeText = "не считалось";

    public CleanupItem(PreferencesStore store, string key, string group, string title, string detail,
        bool defaultChecked, bool needsAdmin, Func<long>? measure, Func<SweepResult> purge)
    {
        this.store = store;
        this.key = "cleanup." + key;
        Group = group;
        Title = title;
        Detail = detail;
        NeedsAdmin = needsAdmin;

        @checked = store.Get(this.key, defaultChecked);
        this.measure = measure;
        this.purge = purge;
    }

    public string Group { get; }

    public string Title { get; }

    public string Detail { get; }

    public bool NeedsAdmin { get; }

    public bool Measurable => measure is not null;

    public long ScannedBytes { get; private set; }

    public bool Checked
    {
        get => @checked;
        set
        {
            if (Set(ref @checked, value))
                store.Set(key, value);
        }
    }

    public string SizeText
    {
        get => sizeText;
        private set => Set(ref sizeText, value);
    }

    public void MeasureCore() => rawBytes = measure?.Invoke() ?? -1;

    public void PublishMeasure()
    {
        ScannedBytes = rawBytes > 0 ? rawBytes : 0;
        SizeText = measure is null ? "," : rawBytes <= 0 ? "пусто" : SweepResult.Format(rawBytes);
        Raise(nameof(ScannedBytes));
    }

    public SweepResult LastPurge => rawResult;

    public void PurgeCore() => rawResult = purge();

    public void PublishPurge() =>
        SizeText = measure is null
            ? "готово"
            : rawResult.Bytes > 0 ? "очищено " + rawResult.Freed : "пусто";
}

public sealed class CleanupViewModel : NavPage
{
    bool busy;
    string status = "Отметьте, что чистить, и нажмите «Просканировать».";

    public CleanupViewModel(PreferencesStore store) : base("Обслуживание", "Обслуживание", "#38BDF8")
    {
        Items = Build(store);
        Scan = new RelayCommand(async _ => await ScanAll(), _ => !busy);
        Clean = new RelayCommand(async _ => await CleanSelected(), _ => !busy && Items.Any(i => i.Checked));
    }

    public ObservableCollection<CleanupItem> Items { get; }

    public ICommand Scan { get; }

    public ICommand Clean { get; }

    public bool Busy
    {
        get => busy;
        private set
        {
            if (Set(ref busy, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string Status
    {
        get => status;
        private set => Set(ref status, value);
    }

    async Task ScanAll()
    {
        Busy = true;
        Status = "Сканирую…";

        var items = Items.ToArray();
        await Task.Run(() => Array.ForEach(items, i => i.MeasureCore()));

        var total = 0L;
        foreach (var item in items)
        {
            item.PublishMeasure();
            if (item.Checked)
                total += item.ScannedBytes;
        }

        Status = total > 0
            ? "К очистке отмечено: " + SweepResult.Format(total)
            : "По отмеченному оценивать нечего.";
        Busy = false;
    }

    async Task CleanSelected()
    {
        var picked = Items.Where(i => i.Checked).ToArray();
        if (picked.Length == 0)
            return;

        Busy = true;
        Status = "Очищаю…";

        await Task.Run(() => Array.ForEach(picked, i => i.PurgeCore()));

        var freed = 0L;
        var files = 0;
        foreach (var item in picked)
        {
            item.PublishPurge();
            freed += item.LastPurge.Bytes;
            files += item.LastPurge.Files;
        }

        Status = freed > 0
            ? "Освобождено: " + SweepResult.Format(freed) + "  ·  файлов удалено: " + files
            : "Готово.";
        Busy = false;
    }

    static ObservableCollection<CleanupItem> Build(PreferencesStore store)
    {
        var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var sysDrive = AutoCleaner.SystemDriveRoot();

        var temp = Path.GetTempPath();
        var winTemp = Path.Combine(windows, "Temp");
        var updateCache = Path.Combine(windows, "SoftwareDistribution", "Download");
        var explorerCache = Path.Combine(local, "Microsoft", "Windows", "Explorer");
        var prefetch = Path.Combine(windows, "Prefetch");
        var edgeCache = Path.Combine(local, "Microsoft", "Edge", "User Data", "Default", "Cache");
        var chromeCache = Path.Combine(local, "Google", "Chrome", "User Data", "Default", "Cache");
        var winOld = Path.Combine(sysDrive, "Windows.old");
        var logs = Path.Combine(windows, "Logs");
        var werSystem = Path.Combine(programData, "Microsoft", "Windows", "WER");
        var werUser = Path.Combine(local, "Microsoft", "Windows", "WER");
        var minidump = Path.Combine(windows, "Minidump");
        var memoryDmp = Path.Combine(windows, "Memory.dmp");

        return
        [
            new(store, "temp", "Временные файлы и кэш", "Временные файлы",
                "%TEMP% и " + winTemp + ". Занятые файлы пропускаются.", true, true,
                () => AutoCleaner.DirBytes(temp, winTemp),
                () => AutoCleaner.PurgeDirs(temp, winTemp)),

            new(store, "update-cache", "Временные файлы и кэш", "Кэш обновлений",
                "Скачанные пакеты в SoftwareDistribution\\Download. Заполнится заново при обновлениях.", true, true,
                () => AutoCleaner.DirBytes(updateCache),
                () => AutoCleaner.PurgeDirs(updateCache)),

            new(store, "thumbs", "Временные файлы и кэш", "Эскизы и кэш иконок",
                "thumbcache и iconcache в кэше Проводника. Пересоздаются по мере надобности.", true, false,
                () => AutoCleaner.GlobBytes(explorerCache, "thumbcache_*.db")
                    + AutoCleaner.GlobBytes(explorerCache, "iconcache_*.db"),
                () =>
                {
                    var a = AutoCleaner.PurgeGlob(explorerCache, "thumbcache_*.db");
                    var b = AutoCleaner.PurgeGlob(explorerCache, "iconcache_*.db");
                    return new SweepResult(a.Files + b.Files, a.Bytes + b.Bytes);
                }),

            new(store, "browser-cache", "Временные файлы и кэш", "Кэш браузеров",
                "Кэш страниц Edge и Chrome (профиль по умолчанию). История и пароли не трогаются.", false, false,
                () => AutoCleaner.DirBytes(edgeCache, chromeCache),
                () => AutoCleaner.PurgeDirs(edgeCache, chromeCache)),

            new(store, "prefetch", "Временные файлы и кэш", "Prefetch",
                "Windows\\Prefetch. Windows заполнит его заново при последующих запусках программ.", false, true,
                () => AutoCleaner.DirBytes(prefetch),
                () => AutoCleaner.PurgeDirs(prefetch)),

            new(store, "logs", "Мусор обновлений", "Журналы обслуживания",
                "Windows\\Logs: журналы CBS, DISM и обновлений.", true, true,
                () => AutoCleaner.DirBytes(logs),
                () => AutoCleaner.PurgeDirs(logs)),

            new(store, "windows-old", "Мусор обновлений", "Папка Windows.old",
                "Файлы прежней версии Windows после обновления. После удаления откат к ней станет невозможен.", false, true,
                () => AutoCleaner.DirBytes(winOld),
                () => AutoCleaner.PurgeDirs(winOld)),

            new(store, "dumps", "Системный мусор", "Дампы памяти",
                "Memory.dmp и минидампы, оставшиеся после сбоев системы.", true, true,
                () => AutoCleaner.FileBytes(memoryDmp) + AutoCleaner.DirBytes(minidump),
                () =>
                {
                    var a = AutoCleaner.DeleteFile(memoryDmp);
                    var b = AutoCleaner.PurgeDirs(minidump);
                    return new SweepResult(a.Files + b.Files, a.Bytes + b.Bytes);
                }),

            new(store, "wer", "Системный мусор", "Отчёты об ошибках",
                "Очереди и архивы Windows Error Reporting (WER).", true, false,
                () => AutoCleaner.DirBytes(werSystem, werUser),
                () => AutoCleaner.PurgeDirs(werSystem, werUser)),

            new(store, "recycle", "Системный мусор", "Корзина",
                "Очистить корзину на всех дисках. Восстановить файлы будет нельзя.", false, false,
                AutoCleaner.RecycleBinBytes,
                AutoCleaner.PurgeRecycleBin),

            new(store, "hibernate", "Гибернация", "Отключить гибернацию",
                "powercfg /hibernate off удаляет hiberfil.sys (около 40% от объёма ОЗУ). Отключит режим сна-в-файл "
              + "и быстрый запуск. Обратимо командой powercfg /hibernate on.", false, true,
                () => AutoCleaner.FileBytes(Path.Combine(sysDrive, "hiberfil.sys")),
                AutoCleaner.DisableHibernation),
        ];
    }
}
