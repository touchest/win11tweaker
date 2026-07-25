using System.Windows;
using System.Windows.Controls;
using Win11Tweaker.App.Shell;

namespace Win11Tweaker.App.Views;

public partial class TaskListView : UserControl
{
    public TaskListView() => InitializeComponent();

    async void ApplyChanges(object sender, RoutedEventArgs e)
    {
        if (DataContext is TaskSectionViewModel section && section.CanApply)
            await section.ApplyAsync();
    }
}
