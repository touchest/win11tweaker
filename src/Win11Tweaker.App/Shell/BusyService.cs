namespace Win11Tweaker.App.Shell;

public sealed class BusyService : ObservableBase
{
    public static BusyService Instance { get; } = new();

    BusyService() { }

    bool active;
    string title = string.Empty;
    string detail = string.Empty;

    public bool Active
    {
        get => active;
        private set => Set(ref active, value);
    }

    public string Title
    {
        get => title;
        private set => Set(ref title, value);
    }

    public string Detail
    {
        get => detail;
        private set => Set(ref detail, value);
    }

    public void Show(string title, string detail)
    {
        Title = title;
        Detail = detail;
        Active = true;
    }

    public void Hide() => Active = false;
}
