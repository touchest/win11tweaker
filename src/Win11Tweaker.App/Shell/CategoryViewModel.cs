using System.Collections.Generic;

namespace Win11Tweaker.App.Shell;

public interface IActivate
{
    void Activate();
}

public sealed class CategoryViewModel : NavPage, ILivePage
{
    NavPage selectedTab;

    public CategoryViewModel(string title, string group, string accentHex, IReadOnlyList<NavPage> tabs)
        : base(title, group, accentHex)
    {
        Tabs = tabs;
        selectedTab = tabs[0];
        (selectedTab as IActivate)?.Activate();
    }

    public IReadOnlyList<NavPage> Tabs { get; }

    public bool HasTabs => Tabs.Count > 1;

    public NavPage SelectedTab
    {
        get => selectedTab;
        set
        {
            if (Set(ref selectedTab, value))
                (value as IActivate)?.Activate();
        }
    }

    public void Enter() => (selectedTab as IActivate)?.Activate();

    public void Tick() => (selectedTab as ILivePage)?.Tick();
}
