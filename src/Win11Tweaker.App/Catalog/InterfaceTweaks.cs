using Microsoft.Win32;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Catalog;

public sealed record TweakGroupSpec(string Title, IReadOnlyList<TweakDefinition> Tweaks);

public static class InterfaceTweaks
{
    const string Advanced = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    const string Search = @"Software\Microsoft\Windows\CurrentVersion\Search";
    const string PushNotifications = @"Software\Microsoft\Windows\CurrentVersion\PushNotifications";
    const string ExplorerPolicy = @"Software\Policies\Microsoft\Windows\Explorer";

    public static IReadOnlyList<TweakGroupSpec> Build() =>
    [
        new TweakGroupSpec("Проводник",
        [
            new TweakDefinition
            {
                Id = "ui.file-extensions",
                Title = "Показывать расширения файлов",
                Summary = "Видно .exe и .scr там, где их прячут. Заодно защищает от файлов вида «отчёт.pdf.exe».",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(Advanced, "HideFileExt", on: 0, off: 1)]
            },
            new TweakDefinition
            {
                Id = "ui.hidden-files",
                Title = "Показывать скрытые файлы",
                Summary = "Скрытые папки становятся видны. Защищённые системные файлы остаются спрятанными.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(Advanced, "Hidden", on: 1, off: 2)]
            },
            new TweakDefinition
            {
                Id = "ui.launch-to-this-pc",
                Title = "Открывать проводник в «Этот компьютер»",
                Summary = "Вместо панели быстрого доступа с недавними файлами.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(Advanced, "LaunchTo", on: 1, off: 2)]
            },
            new TweakDefinition
            {
                Id = "ui.compact-mode",
                Title = "Компактный вид списка",
                Summary = "Возвращает плотность строк Windows 10 - на экран влезает заметно больше.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(Advanced, "UseCompactMode", on: 1, off: 0)]
            },
            new TweakDefinition
            {
                Id = "expl.thispc-folders",
                Title = "Убрать папки из проводника",
                Summary = "Скрывает пользовательские папки (Рабочий стол, Документы, Загрузки, Музыка, Изображения, Видео, Объёмные объекты) из окна «Этот компьютер» и из области переходов. Требует перезапуска проводника.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes =
                [
                    .. ThisPcNode("{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}", "CLSID_ThisPCDesktopRegFolder"),
                    .. ThisPcNode("{d3162b92-9365-467a-956b-92703aca08af}", "CLSID_ThisPCLocalDocumentsRegFolder"),
                    .. ThisPcNode("{A8CDFF1C-4878-43be-B5FD-F8091C1C60D0}", "CLSID_ThisPCDocumentsRegFolder"),
                    .. ThisPcNode("{088e3905-0323-4b02-9826-5d99428e115f}", "CLSID_ThisPCLocalDownloadsRegFolder"),
                    .. ThisPcNode("{374DE290-123F-4565-9164-39C4925E467B}", "CLSID_ThisPCDownloadsRegFolder"),
                    .. ThisPcNode("{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}", "CLSID_ThisPCLocalMusicRegFolder"),
                    .. ThisPcNode("{1CF1260C-4DD0-4ebb-811F-33C572699FDE}", "CLSID_ThisPCMyMusicRegFolder"),
                    .. ThisPcNode("{24ad3ad4-a569-4530-98e1-ab02f9417aa8}", "CLSID_ThisPCLocalPicturesRegFolder"),
                    .. ThisPcNode("{3ADD1653-EB32-4cb0-BBD7-DFA0ABB5ACCA}", "CLSID_ThisPCMyPicturesRegFolder"),
                    .. ThisPcNode("{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}", "CLSID_ThisPCLocalVideosRegFolder"),
                    .. ThisPcNode("{A0953C92-50DC-43bf-BE83-3742FED03C9C}", "CLSID_ThisPCMyVideosRegFolder"),
                    .. ThisPcNode("{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}", "CLSID_ThisPCLocal3DObjectsRegFolder"),

                    NavPin(Clsid, "{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}"),
                    NavPin(Clsid32, "{B4BFCC3A-DB2C-424C-B029-7FE99A87C641}"),
                    NavPin(Clsid, "{088e3905-0323-4b02-9826-5d99428e115f}"),
                    NavPin(Clsid32, "{088e3905-0323-4b02-9826-5d99428e115f}"),
                    NavPin(Clsid, "{d3162b92-9365-467a-956b-92703aca08af}"),
                    NavPin(Clsid32, "{d3162b92-9365-467a-956b-92703aca08af}"),
                    NavPin(Clsid, "{24ad3ad4-a569-4530-98e1-ab02f9417aa8}"),
                    NavPin(Clsid32, "{24ad3ad4-a569-4530-98e1-ab02f9417aa8}"),
                    NavPin(Clsid, "{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}"),
                    NavPin(Clsid32, "{3dfdf296-dbec-4fb4-81d1-6a3438bcf4de}"),
                    NavPin(Clsid, "{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}"),
                    NavPin(Clsid32, "{f86fa3ab-70d2-4fc7-9c99-fcbf05467f3a}"),
                    NavPin(Clsid, "{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}"),
                    NavPin(Clsid32, "{0DB7E03F-FC29-4DC6-9020-FF41B59E513A}")
                ]
            },
            new TweakDefinition
            {
                Id = "expl.network",
                Title = "Убрать «Сеть» из проводника",
                Summary = "Скрывает значок «Сеть» из области переходов проводника. Требует перезапуска проводника.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes =
                [
                    NavPin(Clsid, "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}"),
                    NavPin(Clsid32, "{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}")
                ]
            },
            new TweakDefinition
            {
                Id = "expl.hide-recent",
                Title = "Скрыть недавние и частые в «Главная»",
                Summary = "Убирает недавние файлы и часто используемые папки из «Главная» / «Быстрый доступ» проводника. Требует перезапуска проводника.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes =
                [
                    Flag(Advanced, "ShowRecent", on: 0, off: 1),
                    Flag(Advanced, "ShowFrequent", on: 0, off: 1)
                ]
            }
        ]),

        new TweakGroupSpec("Панель задач",
        [
            new TweakDefinition
            {
                Id = "ui.taskbar-left",
                Title = "Кнопки панели задач слева",
                Summary = "Раскладка Windows 10: пуск в углу, а не по центру.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(Advanced, "TaskbarAl", on: 0, off: 1)]
            },
            new TweakDefinition
            {
                Id = "ui.hide-task-view",
                Title = "Убрать кнопку «Представление задач»",
                Summary = "Сама функция остаётся, работает по Win+Tab. Исчезает только значок.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(Advanced, "ShowTaskViewButton", on: 0, off: 1)]
            },
            new TweakDefinition
            {
                Id = "ui.hide-widgets",
                Title = "Убрать виджеты",
                Summary = "Панель погоды и новостей вместе с её фоновым процессом.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(Advanced, "TaskbarDa", on: 0, off: 1)]
            },
            new TweakDefinition
            {
                Id = "ui.hide-chat",
                Title = "Убрать чат (Teams) с панели",
                Summary = "Убирает значок чата Microsoft Teams с панели задач вместе с его фоновым процессом.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(Advanced, "TaskbarMn", on: 0, off: 1)]
            },
            new TweakDefinition
            {
                Id = "ui.hide-search-box",
                Title = "Убрать поле поиска",
                Summary = "Поиск продолжает открываться по Win+S, с панели пропадает широкое поле.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(Search, "SearchboxTaskbarMode", on: 0, off: 2)]
            },
            new TweakDefinition
            {
                Id = "ui.clock-seconds",
                Title = "Секунды в часах",
                Summary = "Часы на панели начинают показывать секунды. Стоит немного отрисовки.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                AppliesTo = new BuildRange(22621, int.MaxValue),
                Writes = [Flag(Advanced, "ShowSecondsInSystemClock", on: 1, off: 0)]
            }
        ]),

        new TweakGroupSpec("Оформление",
        [
            new TweakDefinition
            {
                Id = "ui.dark-theme",
                Title = "Тёмная тема",
                Summary = "Переключает систему и приложения разом. Применяется сразу, без перезапуска.",
                Risk = RiskLevel.Safe,
                Writes =
                [
                    Flag(Personalize, "AppsUseLightTheme", on: 0, off: 1),
                    Flag(Personalize, "SystemUsesLightTheme", on: 0, off: 1)
                ]
            },
            new TweakDefinition
            {
                Id = "ui.no-transparency",
                Title = "Отключить прозрачность",
                Summary = "Убирает акрил и Mica. Заметно помогает на встроенной графике.",
                Risk = RiskLevel.Safe,
                Writes = [Flag(Personalize, "EnableTransparency", on: 0, off: 1)]
            },
            new TweakDefinition
            {
                Id = "ui.no-shortcut-suffix",
                Title = "Убрать «- Ярлык» у новых ярлыков",
                Summary = "Новые ярлыки создаются без приписки «- Ярлык». На уже созданные не влияет. Требует перезапуска проводника.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes =
                [
                    new RegistryWrite
                    {
                        Root = RegistryRoot.CurrentUser,
                        Key = @"Software\Microsoft\Windows\CurrentVersion\Explorer",
                        Name = "link",
                        Kind = RegistryValueKind.Binary,
                        OnValue = new byte[] { 0, 0, 0, 0 },
                        OffValue = new byte[] { 0x1E, 0, 0, 0 }
                    }
                ]
            },
            new TweakDefinition
            {
                Id = "ui.thin-scrollbar",
                Title = "Тонкий скролл-бар",
                Summary = "Уменьшает ширину полосы прокрутки. Требует выхода из системы.",
                Risk = RiskLevel.Safe,
                NeedsSignOut = true,
                Writes = [Str(@"Control Panel\Desktop\WindowMetrics", "ScrollWidth", on: "-120", off: "-255")]
            },
            new TweakDefinition
            {
                Id = "ui.computer-on-desktop",
                Title = "Значок «Этот компьютер» на рабочий стол",
                Summary = "Возвращает значок «Этот компьютер» на рабочий стол.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(HideDesktopIcons, "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", on: 0, off: 1)]
            },
            new TweakDefinition
            {
                Id = "ui.fast-taskbar-preview",
                Title = "Ускорить превью на панели задач",
                Summary = "Миниатюра окна при наведении на кнопку панели задач появляется сразу, без задержки.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes = [Flag(Advanced, "ExtendedUIHoverTime", on: 1, off: 400)]
            },
            new TweakDefinition
            {
                Id = "ui.no-shortcut-arrows",
                Title = "Убрать стрелки с ярлыков",
                Summary = "Прозрачная стрелка-оверлей в углу иконок ярлыков убирается (подменяется пустой иконкой). Требует перезапуска проводника.",
                Risk = RiskLevel.Safe,
                NeedsShellRestart = true,
                Writes =
                [
                    new RegistryWrite
                    {
                        Root = RegistryRoot.LocalMachine,
                        Key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Shell Icons",
                        Name = "29",
                        Kind = RegistryValueKind.String,
                        OnValue = @"%windir%\System32\imageres.dll,197",
                        OffValue = null
                    }
                ]
            },

            new TweakDefinition
            {
                Id = "ui.photo-viewer",
                Title = "Вернуть классическую «Просмотр фотографий»",
                Summary = "Возвращает старую «Просмотр фотографий Windows» в список программ. После включения выберите её в «Открыть с помощью» или в параметрах приложений по умолчанию для JPG, PNG и других форматов.",
                Risk = RiskLevel.Safe,
                Writes =
                [
                    PhotoAssoc(".jpg"), PhotoAssoc(".jpeg"), PhotoAssoc(".jpe"), PhotoAssoc(".jfif"),
                    PhotoAssoc(".png"), PhotoAssoc(".bmp"), PhotoAssoc(".gif"),
                    PhotoAssoc(".tif"), PhotoAssoc(".tiff"), PhotoAssoc(".ico"), PhotoAssoc(".wdp")
                ]
            }
        ]),

        new TweakGroupSpec("Уведомления",
        [
            new TweakDefinition
            {
                Id = "ui.no-notifications",
                Title = "Отключить уведомления",
                Summary = "Всплывающие уведомления перестают показываться совсем: ничего не выскакивает из угла экрана.",
                Risk = RiskLevel.Safe,
                Writes =
                [
                    Flag(PushNotifications, "ToastEnabled", on: 0, off: 1),
                    Flag(ExplorerPolicy, "DisableNotificationCenter", on: 1, off: 0)
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

    static RegistryWrite Str(string key, string name, string on, string off) => new()
    {
        Root = RegistryRoot.CurrentUser,
        Key = key,
        Name = name,
        Kind = RegistryValueKind.String,
        OnValue = on,
        OffValue = off
    };

    static RegistryWrite PhotoAssoc(string ext) => new()
    {
        Root = RegistryRoot.LocalMachine,
        Key = @"SOFTWARE\Microsoft\Windows Photo Viewer\Capabilities\FileAssociations",
        Name = ext,
        Kind = RegistryValueKind.String,
        OnValue = "PhotoViewer.FileAssoc.Tiff",
        OffValue = null
    };

    const string HideDesktopIcons = @"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel";
    const string Clsid = @"SOFTWARE\Classes\CLSID";
    const string Clsid32 = @"SOFTWARE\Wow6432Node\Classes\CLSID";
    const string MyComputerNs = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace";
    const string MyComputerNs32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Explorer\MyComputer\NameSpace";

    static RegistryWrite[] ThisPcNode(string clsid, string label) =>
    [
        NsNode(MyComputerNs, clsid, label),
        NsNode(MyComputerNs32, clsid, label)
    ];

    static RegistryWrite NsNode(string nsRoot, string clsid, string label) => new()
    {
        Root = RegistryRoot.LocalMachine,
        Key = nsRoot + @"\" + clsid,
        Kind = RegistryValueKind.String,
        AbsentWhenOn = true,
        OnValue = label,
        OffValue = label
    };

    static RegistryWrite NavPin(string clsidRoot, string clsid) => new()
    {
        Root = RegistryRoot.LocalMachine,
        Key = clsidRoot + @"\" + clsid,
        Name = "System.IsPinnedToNameSpaceTree",
        Kind = RegistryValueKind.DWord,
        OnValue = 0,
        OffValue = 1
    };
}
