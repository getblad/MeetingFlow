namespace MeetingFlow.Monolith.Evals;

public sealed record EvalCase(string CaseId, IReadOnlyList<string> Dishes, string Notes);
