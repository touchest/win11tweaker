using System.Windows.Threading;

namespace Win11Tweaker.App.Shell;

public sealed class ToastViewModel
{
    readonly DispatcherTimer timer;

    public ToastViewModel(string text, string? actionLabel, Action? action, Action<ToastViewModel> close)
    {
        Text = text;
        ActionLabel = actionLabel ?? string.Empty;

        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(actionLabel is null ? 6 : 12)
        };
        timer.Tick += (_, _) => { timer.Stop(); close(this); };
        timer.Start();

        Dismiss = new RelayCommand(_ => { timer.Stop(); close(this); });
        Run = new RelayCommand(_ =>
        {
            action?.Invoke();
            timer.Stop();
            close(this);
        });
    }

    public string Text { get; }

    public string ActionLabel { get; }

    public bool HasAction => ActionLabel.Length > 0;

    public RelayCommand Dismiss { get; }

    public RelayCommand Run { get; }
}
