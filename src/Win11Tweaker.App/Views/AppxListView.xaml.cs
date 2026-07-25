using System.Windows;
using System.Windows.Controls;
using Win11Tweaker.App.Shell;

namespace Win11Tweaker.App.Views;

public partial class AppxListView : UserControl
{
    public AppxListView() => InitializeComponent();

    async void ApplyRemoval(object sender, RoutedEventArgs e)
    {
        if (DataContext is not AppxSectionViewModel section || !section.CanApply)
            return;

        var ask = MessageBox.Show(Window.GetWindow(this)!,
            "Удалить отмеченные приложения? Восстановление возможно только переустановкой из Microsoft Store.",
            "Удаление приложений", MessageBoxButton.OKCancel, MessageBoxImage.Warning);

        if (ask == MessageBoxResult.OK)
            await section.ApplyAsync();
    }
}
