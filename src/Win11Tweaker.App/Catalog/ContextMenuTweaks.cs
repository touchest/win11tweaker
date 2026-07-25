using Microsoft.Win32;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Catalog;

public static class ContextMenuTweaks
{
    const string ClassicMenuShim =
        @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32";

    public static IReadOnlyList<TweakGroupSpec> Build() =>
    [
        new TweakGroupSpec("Вид меню",
        [
            new TweakDefinition
            {
                Id = "menu.classic",
                Title = "Классическое меню Windows 10",
                Summary = "Полное меню правой кнопки без пункта «Показать дополнительные параметры».",
                Risk = RiskLevel.Moderate,
                NeedsShellRestart = true,
                Writes =
                [
                    new RegistryWrite
                    {
                        Root = RegistryRoot.CurrentUser,
                        Key = ClassicMenuShim,
                        Kind = RegistryValueKind.String,
                        PresenceOnly = true,
                        OnValue = string.Empty
                    }
                ]
            },
            new TweakDefinition
            {
                Id = "menu.no-delay",
                Title = "Мгновенное раскрытие меню",
                Summary = "Контекстное меню и подменю раскрываются без задержки. Нужен выход из системы.",
                Risk = RiskLevel.Safe,
                NeedsSignOut = true,
                Writes = [Str(@"Control Panel\Desktop", "MenuShowDelay", on: "0", off: "400")]
            }
        ]),

        new TweakGroupSpec("Пункты меню",
        [
            new TweakDefinition
            {
                Id = "menu.take-ownership",
                Title = "«Стать владельцем»",
                Summary = "Пункт на файлах и папках: забирает владение и полный доступ одним кликом. "
                        + "Удобно для защищённых системных файлов.",
                Risk = RiskLevel.Moderate,
                Writes =
                [
                    Verb(@"SOFTWARE\Classes\*\shell\runas", "Стать владельцем"),
                    Verb(@"SOFTWARE\Classes\*\shell\runas\command",
                        @"cmd.exe /c takeown /f ""%1"" && icacls ""%1"" /grant *S-1-5-32-544:F"),
                    Verb(@"SOFTWARE\Classes\Directory\shell\runas", "Стать владельцем"),
                    Verb(@"SOFTWARE\Classes\Directory\shell\runas\command",
                        @"cmd.exe /c takeown /f ""%1"" /r /d y && icacls ""%1"" /grant *S-1-5-32-544:F /t")
                ]
            },
            new TweakDefinition
            {
                Id = "menu.terminal-here",
                Title = "«Открыть окно команд здесь»",
                Summary = "Пункт на папках и фоне окна: открыть командную строку в текущем каталоге.",
                Risk = RiskLevel.Safe,
                Writes =
                [
                    Verb(@"SOFTWARE\Classes\Directory\shell\win11tweaker_cmd", "Открыть окно команд здесь"),
                    Verb(@"SOFTWARE\Classes\Directory\shell\win11tweaker_cmd\command", @"cmd.exe /s /k pushd ""%V"""),
                    Verb(@"SOFTWARE\Classes\Directory\Background\shell\win11tweaker_cmd", "Открыть окно команд здесь"),
                    Verb(@"SOFTWARE\Classes\Directory\Background\shell\win11tweaker_cmd\command", @"cmd.exe /s /k pushd ""%V""")
                ]
            }
        ])
    ];

    static RegistryWrite Verb(string key, string defaultValue) => new()
    {
        Root = RegistryRoot.LocalMachine,
        Key = key,
        Kind = RegistryValueKind.String,
        PresenceOnly = true,
        OnValue = defaultValue
    };

    static RegistryWrite Str(string key, string name, string on, string off) => new()
    {
        Root = RegistryRoot.CurrentUser,
        Key = key,
        Name = name,
        Kind = RegistryValueKind.String,
        OnValue = on,
        OffValue = off
    };
}
