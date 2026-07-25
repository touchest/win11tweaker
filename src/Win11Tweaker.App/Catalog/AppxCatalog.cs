namespace Win11Tweaker.App.Catalog;

public sealed record AppxAppSpec(string Title, string Summary, string[] Packages, string? UnpinMatch = null);

public sealed record AppxGroupSpec(string Title, IReadOnlyList<AppxAppSpec> Apps);

public static class AppxCatalog
{
    public static IReadOnlyList<AppxGroupSpec> Build() =>
    [
        new AppxGroupSpec("Встроенные приложения",
        [
            new AppxAppSpec("Фотографии", "Просмотрщик картинок. Убирай, если пользуешься другим.",
                ["Microsoft.Windows.Photos"]),
            new AppxAppSpec("Камера", "Приложение веб-камеры.",
                ["Microsoft.WindowsCamera"]),
            new AppxAppSpec("Записки (Sticky Notes)", "Стикеры на рабочем столе.",
                ["Microsoft.MicrosoftStickyNotes"]),
            new AppxAppSpec("Paint", "Стандартный редактор картинок.",
                ["Microsoft.Paint", "Microsoft.MSPaint"]),
            new AppxAppSpec("Калькулятор", "Стандартный калькулятор.",
                ["Microsoft.WindowsCalculator"]),
            new AppxAppSpec("Погода", "Плитка погоды от Bing.",
                ["Microsoft.BingWeather"]),
            new AppxAppSpec("Новости", "Плитка новостей от Bing.",
                ["Microsoft.BingNews"]),
            new AppxAppSpec("Связь с телефоном", "Your Phone / Phone Link.",
                ["Microsoft.YourPhone"]),
            new AppxAppSpec("Clipchamp", "Видеоредактор, идущий из коробки.",
                ["Clipchamp.Clipchamp"]),
            new AppxAppSpec("To Do", "Список задач Microsoft.",
                ["Microsoft.Todos"]),
            new AppxAppSpec("Outlook (новый)", "Веб-обёртка почты, ставится сама.",
                ["Microsoft.OutlookForWindows"])
        ]),

        new AppxGroupSpec("Игры и Xbox",
        [
            new AppxAppSpec("Xbox", "Игровое приложение Xbox.",
                ["Microsoft.GamingApp", "Microsoft.XboxApp"]),
            new AppxAppSpec("Игровая панель Xbox", "Оверлей Win+G.",
                ["Microsoft.XboxGamingOverlay"]),
            new AppxAppSpec("Пасьянсы", "Microsoft Solitaire Collection с рекламой.",
                ["Microsoft.MicrosoftSolitaireCollection"])
        ]),

        new AppxGroupSpec("Помощники и медиа",
        [
            new AppxAppSpec("Получить помощь", "Приложение технической поддержки Microsoft.",
                ["Microsoft.GetHelp"]),
            new AppxAppSpec("Советы", "Приложение с подсказками по Windows.",
                ["Microsoft.Getstarted"]),
            new AppxAppSpec("Центр отзывов", "Приложение отправки отзывов в Microsoft.",
                ["Microsoft.WindowsFeedbackHub"]),
            new AppxAppSpec("Карты", "Приложение карт Windows.",
                ["Microsoft.WindowsMaps"]),
            new AppxAppSpec("Запись голоса", "Диктофон Windows.",
                ["Microsoft.WindowsSoundRecorder"]),
            new AppxAppSpec("Люди", "Приложение контактов.",
                ["Microsoft.People"]),
            new AppxAppSpec("Почта и Календарь", "Встроенные почта и календарь.",
                ["microsoft.windowscommunicationsapps"]),
            new AppxAppSpec("Cortana", "Голосовой помощник.",
                ["Microsoft.549981C3F5F10"]),
            new AppxAppSpec("Быстрая помощь", "Удалённый помощник.",
                ["MicrosoftCorporationII.QuickAssist"]),
            new AppxAppSpec("Power Automate", "Автоматизация задач для настольного ПК.",
                ["Microsoft.PowerAutomateDesktop"]),
            new AppxAppSpec("Кино и ТВ", "Медиаплеер Movies & TV.",
                ["Microsoft.ZuneVideo"]),
            new AppxAppSpec("Groove Music", "Музыкальный проигрыватель.",
                ["Microsoft.ZuneMusic"]),
            new AppxAppSpec("Office (ярлык)", "Ярлык-заглушка для установки Office.",
                ["Microsoft.MicrosoftOfficeHub"]),
            new AppxAppSpec("Skype", "Приложение Skype.",
                ["Microsoft.SkypeApp"]),
            new AppxAppSpec("Teams", "Приложение Microsoft Teams.",
                ["MSTeams", "MicrosoftTeams"]),
            new AppxAppSpec("Портал смешанной реальности", "Mixed Reality Portal.",
                ["Microsoft.MixedReality.Portal"])
        ]),

        new AppxGroupSpec("Виджеты и магазин",
        [
            new AppxAppSpec("Виджеты", "Лента виджетов и новостей (Win+W).",
                ["MicrosoftWindows.Client.WebExperience"]),
            new AppxAppSpec("Microsoft Store", "Магазин приложений. Заодно убираем его значок с панели задач.",
                ["Microsoft.WindowsStore", "Microsoft.StorePurchaseApp"], UnpinMatch: "Store")
        ])
    ];
}
