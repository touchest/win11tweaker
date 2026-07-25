using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Win11Tweaker.App.Catalog;
using Win11Tweaker.App.Interop;

namespace Win11Tweaker.App.Shell;

public enum QuizStage { Intro, Question, Summary }

public sealed class ServiceQuizViewModel : NavPage
{
    readonly IReadOnlyList<ServiceQuestion> questions;
    readonly Dictionary<string, bool> answers = [];

    QuizStage stage = QuizStage.Intro;
    int index;
    string status = string.Empty;

    List<(string Name, int Value)> plan = [];
    int protectedByChain;

    public ServiceQuizViewModel() : base("Службы", "Безопасность", "#FB923C")
    {
        questions = ServiceQuizCatalog.Build();

        Start = new RelayCommand(_ => BeginQuiz());
        Yes = new RelayCommand(_ => Answer(true));
        No = new RelayCommand(_ => Answer(false));
        Back = new RelayCommand(_ => GoBack(), _ => stage == QuizStage.Question && index > 0);
        Apply = new RelayCommand(_ => ApplyPlan(), _ => stage == QuizStage.Summary && PlanCount > 0);
        Restart = new RelayCommand(_ => BeginQuiz());

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
    public string ProgressText => $"Вопрос {index + 1} из {questions.Count}";
    public double ProgressValue => questions.Count == 0 ? 0 : (double)index / questions.Count;

    ServiceQuestion? Current => index >= 0 && index < questions.Count ? questions[index] : null;

    public ObservableCollection<string> PlanNames { get; } = [];

    public int PlanCount => plan.Count(p => p.Value == ServiceControl.Disabled);

    public int RestoreCount => plan.Count(p => p.Value == ServiceControl.Manual);

    public string SummaryText
    {
        get
        {
            var parts = new List<string> { "Отключим служб: " + PlanCount };
            if (RestoreCount > 0)
                parts.Add("вернём: " + RestoreCount);
            if (protectedByChain > 0)
                parts.Add("оставим ради зависимостей: " + protectedByChain);
            return string.Join("   ·   ", parts);
        }
    }

    public string Status
    {
        get => status;
        private set => Set(ref status, value);
    }

    public bool HasStatus => status.Length > 0;

    string introStatus = string.Empty;
    public string IntroStatus
    {
        get => introStatus;
        private set => Set(ref introStatus, value);
    }

    public int DisabledCount => questions
        .SelectMany(q => q.Services)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count(ServiceControl.IsDisabled);

    void RefreshIntro()
    {
        var tracked = questions
            .SelectMany(q => q.Services)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count(ServiceControl.Exists);

        IntroStatus = "Сейчас отключено служб: " + DisabledCount + " из " + tracked + ". "
                    + "Ответьте на " + questions.Count + " коротких вопросов - Win11 Tweaker отключит то, "
                    + "чем вы не пользуетесь, и не тронет то, от чего зависит нужное.";
        Raise(nameof(DisabledCount));
    }

    void BeginQuiz()
    {
        answers.Clear();
        index = 0;
        Status = string.Empty;
        Raise(nameof(HasStatus));
        Stage = QuizStage.Question;
        RaiseQuestion();
    }

    void Answer(bool keep)
    {
        var q = Current;
        if (q is null)
            return;

        answers[q.Id] = keep;

        if (index + 1 < questions.Count)
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
        (plan, protectedByChain) = ServicePlanner.Build(
            answers.Where(a => a.Value).SelectMany(a => Services(a.Key)),
            answers.Where(a => !a.Value).SelectMany(a => Services(a.Key)));

        PlanNames.Clear();
        foreach (var name in plan.Where(p => p.Value == ServiceControl.Disabled)
                                 .Select(p => p.Name)
                                 .OrderBy(s => s, StringComparer.OrdinalIgnoreCase))
            PlanNames.Add(name);

        Raise(nameof(PlanCount));
        Raise(nameof(RestoreCount));
        Raise(nameof(SummaryText));
        CommandManager.InvalidateRequerySuggested();
    }

    IEnumerable<string> Services(string questionId)
        => questions.FirstOrDefault(q => q.Id == questionId)?.Services ?? [];

    void ApplyPlan()
    {
        if (plan.Count == 0)
            return;

        var (done, failed) = ServiceControl.ApplyStarts(plan);

        Status = failed == 0
            ? "Готово. Изменено служб: " + done + ". Вступит в силу после перезагрузки."
            : "Изменено: " + done + ", не удалось: " + failed + " (нужны особые права).";

        Raise(nameof(HasStatus));
        RefreshIntro();
    }
}
