using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Win11Tweaker.App.Shell;

public abstract class ObservableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void Raise([CallerMemberName] string? property = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

    protected bool Set<T>(ref T field, T next, [CallerMemberName] string? property = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, next))
            return false;

        field = next;
        Raise(property);
        return true;
    }
}
