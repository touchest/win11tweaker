using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Win11Tweaker.App.Catalog;
using Win11Tweaker.App.Interop;
using Win11Tweaker.Core;

namespace Win11Tweaker.App.Shell;

public sealed class MasterQuizViewModel : NavPage
{
    readonly ShellViewModel shell;
    readonly PreferencesStore prefs;
    readonly IReadOnlyList<MasterQuestion> questions;
    readonly Dictionary<string, bool> answers = [];

    List<MasterQuestion> pending = [];
    List<MasterQuestion> fresh = [];
    bool everTaken;

    QuizStage stage = QuizStage.Intro;
    int index;
    string status = string.Empty;
    string startLabel = "Начать настройку";
    bool busy;

    List<(string Name, int Value)> servicePlan = [];
    List<(TweakViewModel Tweak, bool Enable)> tweakPlan = [];
    List<AppxRowViewModel> appxPlan = [];
    int protectedByChain;

    public MasterQuizViewModel(ShellViewModel shell) : base("Мастер", "Система", "#818CF8")
    {
        this.shell = shell;
        prefs = shell.Preferences;
        questions = MasterQuizCatalog.Build();

        foreach (var q in questions)
            if (prefs.Get(SeenKey(q.Id), false))
                answers[q.Id] = prefs.Get(AnswerKey(q.Id), false);

        Start = new RelayCommand(_ => BeginQuiz(fresh.Count > 0 ? fresh : questions));
        Yes = new RelayCommand(_ => Answer(true));
        No = new RelayCommand(_ => Answer(false));
        Back = new RelayCommand(_ => GoBack(), _ => stage == QuizStage.Question && index > 0);
        Apply = new RelayCommand(_ => _ = ApplyPlanAsync(),
                                 _ => stage == QuizStage.Summary && HasPlan && !busy);
        Restart = new RelayCommand(_ => BeginQuiz(questions));

        RefreshIntro();
    }

    public RelayCommand Start { get; }
    public RelayCommand Yes { get; }
    public RelayCommand No { get; }
    public RelayCommand Back { get; }
    public RelayCommand Apply { get; }
    public RelayCommand Restart { get; }

    public QuizStage Stage
    {
        get => stage;
        private set
        {
            if (Set(ref stage, value))
            {
                Raise(nameof(IsIntro));
                Raise(nameof(IsQuestion));
                Raise(nameof(IsSummary));
            }
        }
    }

    public bool IsIntro => stage == QuizStage.Intro;
    public bool IsQuestion => stage == QuizStage.Question;
    public bool IsSummary => stage == QuizStage.Summary;

    public string SectionText => Current?.Section ?? string.Empty;
    public string QuestionText => Current?.Question ?? string.Empty;
    public string HintText => Current?.Hint ?? string.Empty;
    public string ProgressText => $"Вопрос {index + 1} из {pending.Count}";
    public double ProgressValue => pending.Count == 0 ? 0 : (double)index / pending.Count;

    MasterQuestion? Current => index >= 0 && index < pending.Count ? pending[index] : null;

    public ObservableCollection<string> ServiceNames { get; } = [];
    public ObservableCollection<string> TweakNames { get; } = [];
    public ObservableCollection<string> AppNames { get; } = [];

    public bool HasServices => ServiceNames.Count > 0;
    public bool HasTweaks => TweakNames.Count > 0;
    public bool HasApps => AppNames.Count > 0;

    bool HasPlan => servicePlan.Count > 0 || tweakPlan.Count > 0 || appxPlan.Count > 0;

    public string SummaryText
    {
        get
        {
            var disable = servicePlan.Count(p => p.Value == ServiceControl.Disabled);
            var restore = servicePlan.Count(p => p.Value == ServiceControl.Manual)
                        + tweakPlan.Count(t => !t.Enable);

            var parts = new List<string>();
            if (disable > 0)
                parts.Add("отключим служб: " + disable);
            if (TweakNames.Count > 0)
                parts.Add("твиков: " + tweakPlan.Count(t => t.Enable));
            if (AppNames.Count > 0)
                parts.Add("удалим приложений: " + appxPlan.Count);
            if (restore > 0)
                parts.Add("вернём: " + restore);
            if (protectedByChain > 0)
                parts.Add("оставим ради зависимостей: " + protectedByChain);

            return parts.Count == 0
                ? "Менять нечего: всё уже настроено под ваши ответы."
                : char.ToUpper(parts[0][0]) + parts[0][1..] + (parts.Count > 1
                    ? "   ·   " + string.Join("   ·   ", parts.Skip(1))
                    : string.Empty);
        }
    }

    public string Status
    {
        get => status;
        private set => Set(ref status, value);
    }

    public bool HasStatus => status.Length > 0;

    string introTitle = "Настроить всё разом";
    public string IntroTitle
    {
        get => introTitle;
        private set => Set(ref introTitle, value);
    }

    string introStatus = string.Empty;
    public string IntroStatus
    {
        get => introStatus;
        private set => Set(ref introStatus, value);
    }

    public string StartLabel
    {
        get => startLabel;
        private set => Set(ref startLabel, value);
    }

    void RefreshIntro()
    {
        var seen = questions.Where(q => prefs.Get(SeenKey(q.Id), false))
                            .Select(q => q.Id)
                            .ToHashSet();
        everTaken = seen.Count > 0;
        fresh = questions.Where(q => !seen.Contains(q.Id)).ToList();

        var allTweaks = shell.TweakSections.SelectMany(s => s.Tweaks).ToArray();
        var applied = allTweaks.Count(t => t.Observed == TweakState.On);
        var servicesOff = shell.Services.DisabledCount;
        var configured = applied >= allTweaks.Length / 4 || servicesOff >= 8;

        if (everTaken && fresh.Count > 0)
        {
            IntroTitle = "Появились новые вопросы";
            IntroStatus = "С прошлого раза добавились новые вопросы: " + fresh.Count + ". Ответьте "
                        + "только на них - остальное Win11 Tweaker помнит и поддерживает сам.";
            StartLabel = "Донастроить (" + fresh.Count + ")";
        }
        else if (everTaken)
        {
            IntroTitle = "Всё настроено";
            IntroStatus = "Все ваши ответы учтены, новых вопросов нет. Win11 Tweaker сам доводит систему "
                        + "до этого состояния, в том числе после обновлений Windows.";
            StartLabel = "Пройти заново";
        }
        else if (configured)
        {
            IntroTitle = "Win11 Tweaker не знает ваших ответов";
            IntroStatus = "На машине уже применено твиков: " + applied + ", отключено служб: " + servicesOff
                        + " - но опрос ни разу не пройден, поэтому Win11 Tweaker не знает, что из этого ваш "
                        + "осознанный выбор. Ответьте на вопросы: она сверится с реальным состоянием, "
                        + "доведёт недостающее и дальше будет держать настройки. Уже сделанное не тронет.";
            StartLabel = "Ответить на вопросы";
        }
        else
        {
            IntroTitle = "Настроить всё разом";
            IntroStatus = "Вопросов: " + questions.Count + ". Один проход настроит службы, твики и "
                        + "встроенные приложения разом. Любой пункт потом вернёте тумблером на "
                        + "странице раздела.";
            StartLabel = "Начать настройку";
        }

        Raise(nameof(IntroTitle));
        Raise(nameof(IntroStatus));
        Raise(nameof(StartLabel));
    }

    static string SeenKey(string id) => "master.seen." + id;
    static string AnswerKey(string id) => "master.ans." + id;

    void BeginQuiz(IReadOnlyList<MasterQuestion> set)
    {
        pending = set.ToList();
        index = 0;
        Status = string.Empty;
        Raise(nameof(HasStatus));

        if (pending.Count == 0)
        {
            ComputePlan();
            Stage = QuizStage.Summary;
            return;
        }

        Stage = QuizStage.Question;
        RaiseQuestion();
    }

    void Answer(bool keep)
    {
        var q = Current;
        if (q is null)
            return;

        answers[q.Id] = keep;

        prefs.Set(SeenKey(q.Id), true);
        prefs.Set(AnswerKey(q.Id), keep);

        if (index + 1 < pending.Count)
        {
            index++;
            RaiseQuestion();
        }
        else
        {
            ComputePlan();
            Stage = QuizStage.Summary;
        }
    }

    void GoBack()
    {
        if (index == 0)
            return;
        index--;
        RaiseQuestion();
    }

    void RaiseQuestion()
    {
        Raise(nameof(SectionText));
        Raise(nameof(QuestionText));
        Raise(nameof(HintText));
        Raise(nameof(ProgressText));
        Raise(nameof(ProgressValue));
        CommandManager.InvalidateRequerySuggested();
    }

    void ComputePlan()
    {
        (servicePlan, protectedByChain) = ServicePlanner.Build(
            Picked(true, q => q.Services),
            Picked(false, q => q.Services));

        var enableIds = IdSet(acted: true, q => q.TweakIds);
        var restoreIds = IdSet(acted: false, q => q.TweakIds);
        restoreIds.ExceptWith(enableIds);

        var allTweaks = shell.TweakSections.SelectMany(s => s.Tweaks).ToArray();
        tweakPlan =
        [
            .. allTweaks.Where(t => enableIds.Contains(t.Spec.Id) && t.IsAvailable && !t.IsEnabled)
                        .Select(t => (t, true)),
            .. allTweaks.Where(t => restoreIds.Contains(t.Spec.Id) && t.IsAvailable && t.IsEnabled)
                        .Select(t => (t, false)),
        ];

        var appTitles = IdSet(acted: true, q => q.AppxTitles);
        appxPlan = shell.Apps.Rows
            .Where(r => r.Installed && appTitles.Contains(r.Title))
            .ToList();

        ServiceNames.Clear();
        foreach (var name in servicePlan.Where(p => p.Value == ServiceControl.Disabled)
                                        .Select(p => p.Name)
                                        .OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            ServiceNames.Add(name);

        TweakNames.Clear();
        foreach (var (tweak, enable) in tweakPlan)
            TweakNames.Add(enable ? tweak.Title : tweak.Title + " (вернём)");

        AppNames.Clear();
        foreach (var row in appxPlan)
            AppNames.Add(row.Title);

        Raise(nameof(SummaryText));
        Raise(nameof(HasServices));
        Raise(nameof(HasTweaks));
        Raise(nameof(HasApps));
        CommandManager.InvalidateRequerySuggested();
    }

    IEnumerable<string> Picked(bool keep, Func<MasterQuestion, IReadOnlyList<string>> pick)
        => answers.Where(a => a.Value == keep)
                  .SelectMany(a => questions.Where(q => q.Id == a.Key).SelectMany(pick));

    HashSet<string> IdSet(bool acted, Func<MasterQuestion, IReadOnlyList<string>> pick)
        => new(answers.SelectMany(a => questions
                          .Where(q => q.Id == a.Key && (a.Value == q.ActOnYes) == acted)
                          .SelectMany(pick)),
               StringComparer.Ordinal);

    async System.Threading.Tasks.Task ApplyPlanAsync()
    {
        if (!HasPlan || busy)
            return;

        busy = true;
        Status = "Применяем…";
        Raise(nameof(HasStatus));
        CommandManager.InvalidateRequerySuggested();

        var parts = new List<string>();

        foreach (var (tweak, enable) in tweakPlan)
            tweak.IsEnabled = enable;

        var written = 0;
        foreach (var section in shell.TweakSections.Where(s => s.HasPending))
            written += section.Apply().Written;
        if (written > 0)
            parts.Add("записано твик-значений: " + written);

        if (servicePlan.Count > 0)
        {
            var (done, failed) = ServiceControl.ApplyStarts(servicePlan);
            parts.Add(failed == 0
                ? "изменено служб: " + done
                : "изменено служб: " + done + ", не удалось: " + failed);
        }

        if (appxPlan.Count > 0)
        {
            foreach (var row in appxPlan)
                row.IsEnabled = true;
            await shell.Apps.ApplyAsync();
            parts.Add("удалено приложений: " + appxPlan.Count);
        }

        shell.AfterApply();

        Status = parts.Count == 0
            ? "Нечего было менять."
            : "Готово: " + string.Join(", ", parts) + ". Службы вступят в силу после перезагрузки.";

        busy = false;
        Raise(nameof(HasStatus));
        RefreshIntro();
        CommandManager.InvalidateRequerySuggested();
    }
}
