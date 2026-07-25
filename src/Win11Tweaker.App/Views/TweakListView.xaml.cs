using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Win11Tweaker.App.Interop;
using Win11Tweaker.App.Shell;

namespace Win11Tweaker.App.Views;

public partial class TweakListView : UserControl
{
    public TweakListView() => InitializeComponent();

    async void ApplyPending(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TweakSectionViewModel section || !section.HasPending)
            return;

        if (section.Blocked is { } reason)
        {
            MessageBox.Show(Window.GetWindow(this)!, reason, "Нельзя применить",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (section.RequiresAdmin && !Elevation.IsAdmin)
        {
            var ask = MessageBox.Show(Window.GetWindow(this)!,
                "Этот раздел меняет защищённые настройки и требует прав администратора.\n"
                + "Перезапустить Win11 Tweaker с повышением? Текущие непринятые переключения сбросятся.",
                "Нужен администратор", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            if (ask == MessageBoxResult.OK)
                Elevation.RelaunchAsAdmin();
            return;
        }

        var rows = section.BuildPlan()
            .Select(p => new PlanRow(p.Tweak.Title, p.Change.Write.Display, p.Change.Before ?? "-", p.Change.After ?? "-"))
            .ToArray();

        if (rows.Length == 0)
            return;

        var plan = new PlanWindow(rows, section.Accent) { Owner = Window.GetWindow(this) };
        if (plan.ShowDialog() != true)
            return;

        BusyService.Instance.Show("Точка восстановления",
            "Создаю контрольную точку Windows перед изменениями. Обычно пара секунд.");
        await Task.Run(() => RestorePoint.TryCreateOnce("Win11 Tweaker - перед изменениями"));
        BusyService.Instance.Hide();

        section.Apply();

        if (Application.Current.MainWindow?.DataContext is ShellViewModel shell)
            shell.AfterApply();
    }

    void Recheck(object sender, RoutedEventArgs e)
    {
        if (DataContext is TweakSectionViewModel section)
            section.RefreshGate();
    }
}
