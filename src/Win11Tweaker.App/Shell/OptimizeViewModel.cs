using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Win11Tweaker.App.Interop;

namespace Win11Tweaker.App.Shell;

public sealed class OptimizeAction : ObservableBase
{
    readonly Func<string> label;
    readonly Func<string> perform;
    bool busy;
    string status;
    string buttonText;

    public OptimizeAction(string title, string detail, string status, Func<string> label, Func<string> perform,
        string? blockingTitle = null, string? blockingDetail = null)
    {
        Title = title;
        Detail = detail;
        this.status = status;
        this.label = label;
        this.perform = perform;
        BlockingTitle = blockingTitle;
        BlockingDetail = blockingDetail;
        buttonText = label();
    }

    public string Title { get; }

    public string Detail { get; }

    public string? BlockingTitle { get; }

    public string? BlockingDetail { get; }

    public bool Busy
    {
        get => busy;
        set
        {
            if (Set(ref busy, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string Status
    {
        get => status;
        set => Set(ref status, value);
    }

    public string ButtonText
    {
        get => buttonText;
        private set => Set(ref buttonText, value);
    }

    public string PerformCore() => perform();

    public void RefreshLabel() => ButtonText = label();
}

public sealed class OptimizeViewModel : NavPage
{
    readonly OptimizeAction edgeUpdate;
    readonly OptimizeAction oneDrive;

    public OptimizeViewModel() : base("Оптимизация", "Обслуживание", "#A3E635")
    {
        edgeUpdate = new OptimizeAction(
            "Вырезать Edge WebView2 и автообновление",
            "Штатно удаляет рантайм WebView2, останавливает и удаляет обе службы EdgeUpdate, сносит их задачи, "
          + "чистит папки и блокирует самовосстановление политикой - иначе апдейтер переустанавливает себя при "
          + "обновлениях. Приложения на WebView2 после этого работать перестанут. Нужны права администратора.",
            "проверяю…",
            () => EdgeUpdateRemoval.Present() ? "Вырезать" : "Вырезано",
            () => EdgeUpdateRemoval.Run(deleteFolder: true).Describe(),
            blockingTitle: "Вырезаю Edge и WebView2",
            blockingDetail: "Удаляю рантайм, службы и задачи обновления. Минуту-две - не выключайте компьютер, "
                          + "ничего не зависло.");

        oneDrive = new OptimizeAction(
            "Удалить OneDrive",
            "Запускает штатный деинсталлятор OneDrive, гасит его процессы, сносит задачи планировщика, "
          + "убирает значок из проводника и автозапуск. При желании OneDrive ставится заново.",
            "проверяю…",
            () => OneDriveRemoval.Present() ? "Удалить" : "Удалено",
            () => OneDriveRemoval.Run().Describe(),
            blockingTitle: "Удаляю OneDrive",
            blockingDetail: "Работает штатный деинсталлятор. Несколько секунд - не выключайте компьютер.");

        Actions = [];

        Run = new RelayCommand(async a => await Perform((OptimizeAction)a!),
            a => a is OptimizeAction { Busy: false });

        var ui = Application.Current.Dispatcher;
        _ = Task.Run(() =>
        {
            var edgePresent = EdgeUpdateRemoval.Present();
            var oneDrivePresent = OneDriveRemoval.Present();
            ui.Invoke(() =>
            {
                edgeUpdate.RefreshLabel();
                edgeUpdate.Status = edgePresent ? "EdgeUpdate активен." : "EdgeUpdate уже вырезан.";
                oneDrive.RefreshLabel();
                oneDrive.Status = oneDrivePresent ? "OneDrive установлен." : "OneDrive не найден.";
            });
        });
    }

    public ObservableCollection<OptimizeAction> Actions { get; }

    public OptimizeAction EdgeUpdate => edgeUpdate;

    public OptimizeAction OneDrive => oneDrive;

    public ICommand Run { get; }

    static async Task Perform(OptimizeAction action)
    {
        action.Busy = true;
        action.Status = "Выполняю…";

        var blocking = action.BlockingTitle is not null;
        if (blocking)
            BusyService.Instance.Show(action.BlockingTitle!, action.BlockingDetail ?? string.Empty);

        try
        {
            var result = await Task.Run(action.PerformCore);
            action.Status = result;
        }
        finally
        {
            if (blocking)
                BusyService.Instance.Hide();
        }

        action.RefreshLabel();
        action.Busy = false;
    }
}
