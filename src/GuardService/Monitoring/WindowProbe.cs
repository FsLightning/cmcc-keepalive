using System.Runtime.InteropServices;
using System.Text;
using GuardService.Models;

namespace GuardService.Monitoring;

public sealed class WindowProbe
{
    private readonly ILogger<WindowProbe> _logger;

    public WindowProbe(ILogger<WindowProbe> logger)
    {
        _logger = logger;
    }

    public Task<WindowSnapshot> ProbeAsync(ProcessSnapshot processSnapshot, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var observedAt = DateTimeOffset.Now;
        if (!processSnapshot.IsRunning || !processSnapshot.ProcessId.HasValue)
        {
            return Task.FromResult(WindowSnapshot.CreateMissing(observedAt, "Process is not running."));
        }

        if (!OperatingSystem.IsWindows())
        {
            _logger.LogWarning("Window probing is only supported on Windows hosts.");
            return Task.FromResult(WindowSnapshot.CreateMissing(observedAt, "Window probing is only supported on Windows hosts."));
        }

        var candidates = EnumerateWindows(processSnapshot.ProcessId.Value);
        if (candidates.Count == 0)
        {
            return Task.FromResult(WindowSnapshot.CreateMissing(observedAt, "No top-level windows were found for the selected process."));
        }

        var orderedCandidates = candidates
            .OrderByDescending(candidate => candidate.IsVisible)
            .ThenBy(candidate => candidate.IsMinimized)
            .ThenByDescending(candidate => candidate.Area)
            .ThenByDescending(candidate => candidate.Handle)
            .ToList();

        var selected = orderedCandidates[0];
        var topScoreCount = orderedCandidates.Count(candidate =>
            candidate.IsVisible == selected.IsVisible &&
            candidate.IsMinimized == selected.IsMinimized &&
            candidate.Area == selected.Area);

        var snapshot = new WindowSnapshot(
            observedAt,
            true,
            topScoreCount == 1,
            selected.Handle,
            selected.HandleHex,
            selected.Title,
            selected.ClassName,
            selected.IsVisible,
            selected.IsMinimized,
            selected.Bounds,
            topScoreCount == 1
                ? "Selected visible, non-minimized, largest-area top-level window."
                : "Selected the best-scoring top-level window, but multiple windows shared the same score.",
            orderedCandidates);

        return Task.FromResult(snapshot);
    }

    private static List<WindowCandidate> EnumerateWindows(int processId)
    {
        var candidates = new List<WindowCandidate>();

        var stateHandle = CreateStateHandle(processId, candidates);

        try
        {
            EnumWindows(static (windowHandle, callbackStateHandle) =>
            {
                var state = GCHandle.FromIntPtr(callbackStateHandle);
                var enumerationState = (WindowEnumerationState)state.Target!;

                GetWindowThreadProcessId(windowHandle, out var currentProcessId);
                if (currentProcessId != enumerationState.ProcessId)
                {
                    return true;
                }

                var title = GetWindowText(windowHandle);
                var className = GetClassName(windowHandle);
                var isVisible = IsWindowVisible(windowHandle);
                var isMinimized = IsIconic(windowHandle);
                if (!GetWindowRect(windowHandle, out var rect))
                {
                    return true;
                }

                var bounds = new WindowBounds(rect.Left, rect.Top, rect.Right, rect.Bottom);
                var area = Math.Max(bounds.Width, 0) * Math.Max(bounds.Height, 0);

                enumerationState.Candidates.Add(new WindowCandidate(
                    windowHandle.ToInt64(),
                    $"0x{windowHandle.ToInt64():X}",
                    title,
                    className,
                    isVisible,
                    isMinimized,
                    bounds,
                    area));

                return true;
            }, stateHandle);
        }
        finally
        {
            GCHandle.FromIntPtr(stateHandle).Free();
        }

        return candidates;
    }

    private static IntPtr CreateStateHandle(int processId, List<WindowCandidate> candidates)
    {
        var handle = GCHandle.Alloc(new WindowEnumerationState(processId, candidates));
        return GCHandle.ToIntPtr(handle);
    }

    private sealed class WindowEnumerationState
    {
        public WindowEnumerationState(int processId, List<WindowCandidate> candidates)
        {
            ProcessId = processId;
            Candidates = candidates;
        }

        public int ProcessId { get; }

        public List<WindowCandidate> Candidates { get; }
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private static string GetWindowText(IntPtr windowHandle)
    {
        var length = GetWindowTextLength(windowHandle);
        if (length == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        GetWindowText(windowHandle, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetClassName(IntPtr windowHandle)
    {
        var builder = new StringBuilder(256);
        GetClassName(windowHandle, builder, builder.Capacity);
        return builder.ToString();
    }
}
