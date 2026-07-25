using Microsoft.Win32;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Catalog;

public static class PerformanceTweaks
{
    const string Advanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    const string Desktop = @"Control Panel\Desktop";
    const string Serialize = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize";
    const string BackgroundApps = @"Software\Microsoft\Windows\CurrentVersion\BackgroundAccessApplications";
    const string Prefetch = @"SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\PrefetchParameters";
    const string DeliveryOpt = @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization";
    const string DesktopIcons =
        @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
    const string ThisPc = "{20D04FE0-3AEA-1069-A2D8-08002B30309D}";

    public static IReadOnlyList<TweakGroupSpec> Build() =>
    [
        new TweakGroupSpec("Отзывчивость",
        [
            new TweakDefinition
            {
                Id = "perf.animations",
                Title = "Отключить анимации интерфейса",
                Summary = "Убирает плавные движения окон, меню и панели задач. Интерфейс начинает отзываться моментально, потерю анимаций почти не замечаешь.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes =
                [
                    new RegistryWrite
                    {
                        Root = RegistryRoot.CurrentUser,
                        Key = Desktop,
                        Name = "MenuShowDelay",
                        Kind = RegistryValueKind.String,
                        OnValue = "0",
                        OffValue = "400"
                    },
                    Flag(Advanced, "TaskbarAnimations", on: 0, off: 1)
                ]
            },
            new TweakDefinition
            {
                Id = "perf.startup-delay",
                Title = "Убрать задержку автозагрузки",
                Summary = "Windows специально тормозит запуск твоих автозагрузочных программ. Отключаем: стартуют сразу.",
                Risk = RiskLevel.Safe,
                Writes = [FlagPolicy(Serialize, "StartupDelayInMSec", on: 0)]
            }
        ]),

        new TweakGroupSpec("Диск, память и сеть",
        [
            new TweakDefinition
            {
                Id = "perf.prefetch",
                Title = "Отключить Prefetch",
                Summary = "Предзагрузка программ в память. На SSD толку почти ноль, а обращений к диску меньше.",
                Risk = RiskLevel.Moderate,
                NeedsSignOut = true,
                Writes =
                [
                    Hklm(Prefetch, "EnablePrefetcher", on: 0, restore: 3),
                    Hklm(Prefetch, "EnableSuperfetch", on: 0, restore: 3)
                ]
            },
            new TweakDefinition
            {
                Id = "perf.background-apps",
                Title = "Отключить фоновые приложения",
                Summary = "Магазинные приложения перестают крутиться в фоне. Учти: пуш-уведомления от них тоже перестанут приходить.",
                Risk = RiskLevel.Moderate,
                Writes = [Flag(BackgroundApps, "GlobalUserDisabled", on: 1, off: 0)]
            },
            new TweakDefinition
            {
                Id = "perf.delivery-optimization",
                Title = "Не раздавать обновления другим ПК",
                Summary = "Windows раздаёт куски обновлений чужим компам через твой интернет. Выключаем: трафик остаётся твоим.",
                Risk = RiskLevel.Safe,
                Writes = [Hklm(DeliveryOpt, "DODownloadMode", on: 0)]
            }
        ]),

        new TweakGroupSpec("Рабочий стол",
        [
            new TweakDefinition
            {
                Id = "perf.show-thispc",
                Title = "Вернуть «Этот компьютер» на рабочий стол",
                Summary = "Возвращает привычный значок «Этот компьютер» на рабочий стол.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(DesktopIcons, ThisPc, on: 0, off: 1)]
            }
        ])
    ];

    static RegistryWrite Flag(string key, string name, int on, int off) => new()
    {
        Root = RegistryRoot.CurrentUser,
        Key = key,
        Name = name,
        Kind = RegistryValueKind.DWord,
        OnValue = on,
        OffValue = off
    };

    static RegistryWrite FlagPolicy(string key, string name, int on) => new()
    {
        Root = RegistryRoot.CurrentUser,
        Key = key,
        Name = name,
        Kind = RegistryValueKind.DWord,
        OnValue = on,
        OffValue = null
    };

    static RegistryWrite Hklm(string key, string name, int on, int? restore = null) => new()
    {
        Root = RegistryRoot.LocalMachine,
        Key = key,
        Name = name,
        Kind = RegistryValueKind.DWord,
        OnValue = on,
        OffValue = restore
    };
}
