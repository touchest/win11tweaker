using Microsoft.Win32;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Catalog;

public static class ExtrasTweaks
{
    const string Services = @"SYSTEM\CurrentControlSet\Services";
    const string GameStore = @"System\GameConfigStore";
    const string GamePolicy = @"SOFTWARE\Policies\Microsoft\Windows\GameDVR";
    const string PowerControl = @"SYSTEM\CurrentControlSet\Control\Session Manager\Power";
    const string ContentDelivery = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
    const string Advanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    const string StorePolicy = @"SOFTWARE\Policies\Microsoft\WindowsStore";

    public static IReadOnlyList<TweakGroupSpec> Build() =>
    [
        new TweakGroupSpec("Игры и устройства",
        [
            new TweakDefinition
            {
                Id = "extra.gamebar",
                Title = "Я не использую Xbox и игровую панель",
                Summary = "Панель Win+G и запись экрана игры (Game DVR).",
                Risk = RiskLevel.Safe,
                Writes =
                [
                    Flag(GameStore, "GameDVR_Enabled", on: 0, off: 1),
                    Hklm(GamePolicy, "AllowGameDVR", on: 0)
                ]
            },
            new TweakDefinition
            {
                Id = "extra.print",
                Title = "Я не использую принтер",
                Summary = "Служба печати (Spooler). Печать станет недоступна до повторного включения.",
                Risk = RiskLevel.Moderate,
                NeedsSignOut = true,
                Writes = [Start("Spooler", off: 2)]
            },
            new TweakDefinition
            {
                Id = "extra.bluetooth",
                Title = "Я не использую Bluetooth",
                Summary = "Отключает Bluetooth целиком. Затрагивает беспроводные мышь, клавиатуру, наушники и колонки.",
                Risk = RiskLevel.Moderate,
                NeedsSignOut = true,
                Writes = [Start("bthserv", off: 3)]
            }
        ]),

        new TweakGroupSpec("Реклама и магазин",
        [
            new TweakDefinition
            {
                Id = "extra.lockscreen-ads",
                Title = "Убрать рекламу с экрана блокировки",
                Summary = "Отключает подборку изображений и «интересные факты» Windows на экране входа.",
                Risk = RiskLevel.Safe,
                Writes =
                [
                    Flag(ContentDelivery, "RotatingLockScreenOverlayEnabled", on: 0, off: 1),
                    Flag(ContentDelivery, "SubscribedContent-338387Enabled", on: 0, off: 1)
                ]
            },
            new TweakDefinition
            {
                Id = "extra.start-ads",
                Title = "Убрать рекомендации и рекламу в меню «Пуск»",
                Summary = "Отключает рекламу приложений и рекомендации в меню «Пуск». Требует перезапуска проводника.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes =
                [
                    Flag(Advanced, "Start_IrisRecommendations", on: 0, off: 1),
                    Flag(ContentDelivery, "SystemPaneSuggestionsEnabled", on: 0, off: 1),
                    Flag(ContentDelivery, "SubscribedContent-338388Enabled", on: 0, off: 1)
                ]
            },
            new TweakDefinition
            {
                Id = "extra.store",
                Title = "Я не пользуюсь Microsoft Store",
                Summary = "Отключает магазин приложений и его фоновые автообновления политикой. Восстанавливается повторным включением.",
                Risk = RiskLevel.Moderate,
                Writes =
                [
                    Hklm(StorePolicy, "RemoveWindowsStore", on: 1),
                    Hklm(StorePolicy, "AutoDownload", on: 2)
                ]
            }
        ]),

        new TweakGroupSpec("Загрузка",
        [
            new TweakDefinition
            {
                Id = "extra.fast-startup",
                Title = "Отключить быстрый запуск",
                Summary = "Гибридное завершение работы. Отключение помогает при проблемах с загрузкой или при второй ОС на том же компьютере.",
                Risk = RiskLevel.Safe,
                Writes = [Hklm(PowerControl, "HiberbootEnabled", on: 0, restore: 1)]
            }
        ]),

        new TweakGroupSpec("Система и сеть",
        [
            new TweakDefinition
            {
                Id = "extra.cortana",
                Title = "Я не использую Cortana",
                Summary = "Полностью выключает голосового ассистента Cortana политикой поиска.",
                Risk = RiskLevel.Safe,
                Writes = [Hklm(@"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", on: 0)]
            },
            new TweakDefinition
            {
                Id = "extra.ipv6",
                Title = "Мне не нужен IPv6, Teredo и ISATAP",
                Summary = "Отключает протокол IPv6 и туннели Teredo/ISATAP. Если сеть работает по IPv4 - безопасно и чуть ускоряет отклик. Требует перезагрузки.",
                Risk = RiskLevel.Moderate,
                NeedsSignOut = true,
                Writes = [Hklm(@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters", "DisabledComponents", on: 0xFF, restore: 0)]
            },
            new TweakDefinition
            {
                Id = "extra.no-autorun",
                Title = "Отключить автозапуск съёмных носителей",
                Summary = "Флешки и диски не запускают автоматически ничего при подключении. Защита от авто-старта заразы.",
                Risk = RiskLevel.Safe,
                Writes = [Hklm(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoDriveTypeAutoRun", on: 0xFF, restore: 0x91)]
            },
            new TweakDefinition
            {
                Id = "extra.no-openfile-warning",
                Title = "Убрать предупреждение при запуске exe",
                Summary = "Windows перестаёт спрашивать «Запустить этот файл?» при открытии загруженных .exe и установщиков.",
                Risk = RiskLevel.Moderate,
                Writes =
                [
                    new RegistryWrite
                    {
                        Root = RegistryRoot.CurrentUser,
                        Key = @"Software\Microsoft\Windows\CurrentVersion\Policies\Associations",
                        Name = "LowRiskFileTypes",
                        Kind = RegistryValueKind.String,
                        OnValue = ".exe;.bat;.cmd;.msi;.reg;.vbs;.ps1;.msu",
                        OffValue = null
                    }
                ]
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

    static RegistryWrite Hklm(string key, string name, int on, int? restore = null) => new()
    {
        Root = RegistryRoot.LocalMachine,
        Key = key,
        Name = name,
        Kind = RegistryValueKind.DWord,
        OnValue = on,
        OffValue = restore
    };

    static RegistryWrite Start(string service, int off) => new()
    {
        Root = RegistryRoot.LocalMachine,
        Key = Services + "\\" + service,
        Name = "Start",
        Kind = RegistryValueKind.DWord,
        OnValue = 4,
        OffValue = off
    };
}
