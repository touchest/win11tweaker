using System.Windows;
using System.Windows.Controls;
using Win11Tweaker.App.Shell;

namespace Win11Tweaker.App.Views;

public partial class AutostartView : UserControl
{
    public AutostartView() => InitializeComponent();

    void Trust(object sender, RoutedEventArgs e)
    {
        if (DataContext is AutostartViewModel vm
            && sender is FrameworkElement { DataContext: AutostartRow row })
            vm.AddToTrusted(row);
    }
}
