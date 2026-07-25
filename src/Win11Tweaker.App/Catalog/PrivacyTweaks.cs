using Microsoft.Win32;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Catalog;

public static class PrivacyTweaks
{
    const string DataCollection = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection";
    const string SystemPolicy = @"SOFTWARE\Policies\Microsoft\Windows\System";
    const string CloudContent = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent";
    const string SearchPolicy = @"SOFTWARE\Policies\Microsoft\Windows\Windows Search";
    const string WindowsAi = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI";
    const string AdInfo = @"Software\Microsoft\Windows\CurrentVersion\AdvertisingInfo";
    const string Privacy = @"Software\Microsoft\Windows\CurrentVersion\Privacy";
    const string Copilot = @"Software\Policies\Microsoft\Windows\WindowsCopilot";

    public static IReadOnlyList<TweakGroupSpec> Build() =>
    [
        new TweakGroupSpec("Слежка",
        [
            new TweakDefinition
            {
                Id = "priv.telemetry",
                Title = "Отключить телеметрию и слежку",
                Summary = "Одной галкой: диагностика на минимум, история действий, индивидуальные предложения и рекламный ID.",
                Risk = RiskLevel.Safe,
                Writes =
                [
                    Hklm(DataCollection, "AllowTelemetry", on: 0),
                    Hklm(SystemPolicy, "EnableActivityFeed", on: 0),
                    Hklm(SystemPolicy, "PublishUserActivities", on: 0),
                    Hklm(SystemPolicy, "UploadUserActivities", on: 0),
                    Hkcu(Privacy, "TailoredExperiencesWithDiagnosticDataEnabled", on: 0, off: 1),
                    Hkcu(AdInfo, "Enabled", on: 0, off: 1)
                ]
            }
        ]),

        new TweakGroupSpec("Реклама",
        [
            new TweakDefinition
            {
                Id = "priv.ads",
                Title = "Убрать рекламу и рекомендации Windows",
                Summary = "Отключает тихую подкачку промо-приложений и веб-подсказки Bing в меню «Пуск».",
                Risk = RiskLevel.Safe,
                Writes =
                [
                    Hklm(CloudContent, "DisableWindowsConsumerFeatures", on: 1),
                    Hklm(SearchPolicy, "DisableWebSearch", on: 1),
                    Hklm(SearchPolicy, "ConnectedSearchUseWeb", on: 0)
                ]
            }
        ]),

        new TweakGroupSpec("ИИ-функции",
        [
            new TweakDefinition
            {
                Id = "priv.copilot",
                Title = "Отключить Copilot",
                Summary = "Убирает кнопку и панель Copilot политикой. Применяется после выхода из системы.",
                Risk = RiskLevel.Safe,
                NeedsSignOut = true,
                Writes = [HkcuValue(Copilot, "TurnOffWindowsCopilot", on: 1)]
            },
            new TweakDefinition
            {
                Id = "priv.recall",
                Title = "Отключить Recall",
                Summary = "Recall перестаёт делать и анализировать снимки экрана. Есть только на Windows 11 24H2 и новее.",
                Risk = RiskLevel.Moderate,
                AppliesTo = new BuildRange(26100, int.MaxValue),
                Writes = [Hklm(WindowsAi, "DisableAIDataAnalysis", on: 1)]
            }
        ])
    ];

    static RegistryWrite Hklm(string key, string name, int on) => new()
    {
        Root = RegistryRoot.LocalMachine,
        Key = key,
        Name = name,
        Kind = RegistryValueKind.DWord,
        OnValue = on,
        OffValue = null
    };

    static RegistryWrite Hkcu(string key, string name, int on, int off) => new()
    {
        Root = RegistryRoot.CurrentUser,
        Key = key,
        Name = name,
        Kind = RegistryValueKind.DWord,
        OnValue = on,
        OffValue = off
    };

    static RegistryWrite HkcuValue(string key, string name, int on) => new()
    {
        Root = RegistryRoot.CurrentUser,
        Key = key,
        Name = name,
        Kind = RegistryValueKind.DWord,
        OnValue = on,
        OffValue = null
    };
}
