namespace GuardService.Models;

public enum ProcessDetectionState
{
    NotRunning,
    Running,
}

public enum SessionState
{
    NotRunning,
    ProcessOnly,
    ClientVisibleButUnknown,
    DesktopReady,
}

public enum CycleActionType
{
    StartProcess,
    LoginClick,
    KillProcess,
    Skip,
}

public sealed record CycleAction(
    DateTimeOffset ObservedAt,
    CycleActionType Type,
    bool Succeeded,
    string Message,
    string? Method = null,
    string? Details = null);

public sealed record ProcessCandidate(
    int ProcessId,
    string? ExecutablePath,
    DateTimeOffset? StartTime,
    bool HasMainWindow,
    string SelectionReason);

public sealed record ProcessSnapshot(
    DateTimeOffset ObservedAt,
    string ConfiguredProcessName,
    ProcessDetectionState State,
    int? ProcessId,
    string? ExecutablePath,
    DateTimeOffset? StartTime,
    bool HasMainWindow,
    string? SelectionReason,
    IReadOnlyList<ProcessCandidate> Candidates)
{
    public bool IsRunning => State == ProcessDetectionState.Running && ProcessId.HasValue;

    public static ProcessSnapshot CreateNotRunning(DateTimeOffset observedAt, string configuredProcessName, IReadOnlyList<ProcessCandidate>? candidates = null)
    {
        return new ProcessSnapshot(
            observedAt,
            configuredProcessName,
            ProcessDetectionState.NotRunning,
            null,
            null,
            null,
            false,
            "No matching process instance was found.",
            candidates ?? []);
    }
}

public sealed record WindowBounds(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;
}

public sealed record WindowCandidate(
    long Handle,
    string HandleHex,
    string Title,
    string ClassName,
    bool IsVisible,
    bool IsMinimized,
    WindowBounds Bounds,
    int Area);

public sealed record WindowSnapshot(
    DateTimeOffset ObservedAt,
    bool HasWindow,
    bool IsSelectionDeterministic,
    long? Handle,
    string? HandleHex,
    string? Title,
    string? ClassName,
    bool? IsVisible,
    bool? IsMinimized,
    WindowBounds? Bounds,
    string? SelectionReason,
    IReadOnlyList<WindowCandidate> Candidates)
{
    public static WindowSnapshot CreateMissing(DateTimeOffset observedAt, string reason, IReadOnlyList<WindowCandidate>? candidates = null)
    {
        return new WindowSnapshot(
            observedAt,
            false,
            true,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            reason,
            candidates ?? []);
    }
}

public sealed record GuardCycleResult(
    DateTimeOffset ObservedAt,
    ProcessSnapshot Process,
    WindowSnapshot Window,
    SessionState SessionState,
    IReadOnlyList<CycleAction> Actions,
    int? TestLoopsCompleted,
    int? TestLoopsTarget)
{
    public string ToSummary()
    {
        var pid = Process.ProcessId?.ToString() ?? "n/a";
        var hwnd = Window.HandleHex ?? "n/a";
        var title = string.IsNullOrWhiteSpace(Window.Title) ? "n/a" : Window.Title;
        var actionSummary = Actions.Count == 0
            ? "none"
            : string.Join(",", Actions.Select(action => $"{action.Type}:{(action.Succeeded ? "ok" : "fail")}"));
        var testSummary = TestLoopsTarget.HasValue
            ? $"{TestLoopsCompleted ?? 0}/{TestLoopsTarget.Value}"
            : "off";

        return $"state={SessionState}; process={Process.State}; pid={pid}; hwnd={hwnd}; title={title}; actions={actionSummary}; testLoops={testSummary}";
    }
}
