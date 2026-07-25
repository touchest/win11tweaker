using System.Windows;
using System.Windows.Controls;
using Win11Tweaker.App.Shell;

namespace Win11Tweaker.App.Views;

public partial class ProcessView : UserControl
{
    public ProcessView() => InitializeComponent();

    void EndProcess(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ProcessMonitorViewModel vm
            || sender is not FrameworkElement { DataContext: ProcessRow row })
            return;

        var warning = "Завершить «" + row.Name + "» (PID " + row.Pid + ")?\n\n"
                    + "Издатель: " + row.Publisher + "\n"
                    + "Категория: " + row.Category + "\n\n"
                    + "Несохранённые данные пропадут. Если процесс в автозапуске, он вернётся после перезагрузки.";

        var answer = MessageBox.Show(Window.GetWindow(this)!, warning, "Завершение процесса",
            MessageBoxButton.OKCancel, MessageBoxImage.Warning);

        if (answer == MessageBoxResult.OK && !vm.Terminate(row))
            MessageBox.Show(Window.GetWindow(this)!,
                "Не удалось завершить. Процесс мог закрыться сам или оказался защищённым.",
                "Завершение процесса", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
