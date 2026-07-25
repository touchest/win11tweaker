using System.Collections.Generic;
using System.Linq;

namespace Win11Tweaker.App.Catalog;

public sealed record MasterQuestion(
    string Id,
    string Section,
    string Question,
    string Hint,
    IReadOnlyList<string> Services,
    IReadOnlyList<string> TweakIds,
    IReadOnlyList<string> AppxTitles,
    bool ActOnYes = false,
    bool Autoclean = false);

public static class MasterQuizCatalog
{
    static readonly Dictionary<string, (string[] Tweaks, string[] Apps)> Supplements = new()
    {
        ["q.xbox"] = (["extra.gamebar"], ["Xbox", "Игровая панель Xbox", "Пасьянсы"]),
        ["q.maps"] = ([], ["Карты"]),
        ["q.phone-link"] = ([], ["Связь с телефоном"]),
        ["q.telemetry"] = (["priv.telemetry"], []),
        ["q.error-reports"] = ([], ["Центр отзывов"]),
        ["q.updates"] = (["perf.delivery-optimization"], []),

        ["q.sysmain"] = (["perf.prefetch"], []),
    };

    static IReadOnlyList<MasterQuestion> Extras() =>
    [
        new("m.media-apps", "Игры и медиа",
            "Смотрите ли вы видео и слушаете музыку встроенными «Кино и ТВ» и Groove?",
            "Плееры Microsoft из коробки. Большинство пользуется другими.",
            [], [], ["Кино и ТВ", "Groove Music"]),

        new("m.ads", "Приватность",
            "Хотите ли вы видеть рекламу и «предложения» Windows?",
            "Промо на экране блокировки, рекомендации в Пуске, рекламный идентификатор.",
            [], ["priv.ads", "extra.lockscreen-ads", "extra.start-ads"], []),

        new("m.copilot", "Приватность",
            "Пользуетесь ли вы Copilot и функцией Recall?",
            "ИИ-помощник Windows и снимки экрана для «памяти» системы.",
            [], ["priv.copilot", "priv.recall"], []),

        new("m.background", "Обслуживание",
            "Нужны ли вам фоновые приложения из Store (уведомления, живые плитки)?",
            "«Нет» запретит UWP-приложениям крутиться в фоне.",
            [], ["perf.background-apps"], []),

        new("m.startup-delay", "Обслуживание",
            "Убрать искусственную задержку автозагрузки при старте Windows?",
            "Windows специально тормозит запуск ваших автозагрузочных программ. Безопасно.",
            [], ["perf.startup-delay"], [], ActOnYes: true),

        new("m.fast-startup", "Обслуживание",
            "Отключить «быстрый запуск» (гибридное завершение работы)?",
            "Лечит проблемы с загрузкой и нужен при второй ОС. Если всё грузится нормально - «Нет».",
            [], ["extra.fast-startup"], [], ActOnYes: true),

        new("m.ipv6", "Сеть",
            "Нужен ли вам IPv6 (включая туннели Teredo/ISATAP)?",
            "Не уверены - отвечайте «Да». Отключение требует перезагрузки.",
            [], ["extra.ipv6"], []),

        new("m.cortana", "Функции",
            "Пользуетесь ли вы голосовым помощником Cortana?",
            "В России и Европе толком не работает - смело «Нет».",
            [], ["extra.cortana"], ["Cortana"]),

        new("m.widgets", "Функции",
            "Пользуетесь ли вы виджетами и лентой новостей (Win+W)?",
            "Панель виджетов и новости Bing на панели задач.",
            [], ["ui.hide-widgets"], ["Виджеты", "Новости"]),

        new("m.store", "Функции",
            "Устанавливаете ли вы приложения из Microsoft Store?",
            "«Нет» уберёт магазин и его значок. Вернуть сложно - отвечайте честно.",
            [], ["extra.store"], ["Microsoft Store"]),

        new("m.teams-skype", "Функции",
            "Пользуетесь ли вы Teams или Skype?",
            "Встроенные мессенджеры Microsoft.",
            [], [], ["Teams", "Skype"]),

        new("m.office-mail", "Функции",
            "Пользуетесь ли вы встроенной почтой и ярлыками Office?",
            "«Почта и Календарь», новый Outlook и заглушка установки Office.",
            [], [], ["Office (ярлык)", "Outlook (новый)", "Почта и Календарь"]),

        new("m.help-apps", "Функции",
            "Пользуетесь ли вы встроенными помощниками Windows?",
            "«Получить помощь», «Советы», Power Automate, «Быстрая помощь», портал MR.",
            [], [], ["Получить помощь", "Советы", "Power Automate", "Быстрая помощь", "Портал смешанной реальности"]),

        new("m.autorun", "Функции",
            "Хотите ли вы, чтобы флешки и диски запускались сами при подключении?",
            "Автозапуск со съёмных носителей - частый канал заразы. «Нет» безопаснее.",
            [], ["extra.no-autorun"], []),

        new("m.animations", "Интерфейс",
            "Нравятся ли вам анимации и прозрачность интерфейса Windows?",
            "«Нет» отключит их - меню и окна станут заметно отзывчивее.",
            [], ["perf.animations", "ui.no-transparency"], []),
    ];

    static readonly string[] SectionOrder =
    [
        "Игры и медиа", "Виртуализация", "Устройства", "Сеть",
        "Приватность", "Обслуживание", "Функции", "Интерфейс", "Управление",
    ];

    public static IReadOnlyList<MasterQuestion> Build()
    {
        var fromServices = ServiceQuizCatalog.Build()
            .Select(q =>
            {
                var (tweaks, apps) = Supplements.TryGetValue(q.Id, out var extra)
                    ? extra
                    : (Array.Empty<string>(), Array.Empty<string>());
                return new MasterQuestion(q.Id, q.Section, q.Question, q.Hint, q.Services, tweaks, apps);
            })
            .ToArray();

        var extras = Extras();

        return SectionOrder
            .SelectMany(section => fromServices.Where(q => q.Section == section)
                .Concat(extras.Where(q => q.Section == section)))
            .ToArray();
    }
}
