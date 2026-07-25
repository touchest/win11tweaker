using System.Windows.Input;

namespace Win11Tweaker.App.Shell;

public sealed class RelayCommand(Action<object?> run, Func<object?, bool>? allowed = null) : ICommand
{
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => allowed?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => run(parameter);
}
