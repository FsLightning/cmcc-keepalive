using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("WindowInspector only runs on Windows.");
    return 1;
}

var options = InspectorOptions.Parse(args);
var report = WindowReportBuilder.Build(options);
Directory.CreateDirectory(Path.GetDirectoryName(report.OutputPath)!);
await File.WriteAllTextAsync(report.OutputPath, report.Markdown, new UTF8Encoding(false));
Console.WriteLine($"Window report written to {report.OutputPath}");
Console.WriteLine($"Selected process: PID={report.SelectedProcess.ProcessId}, Name={report.SelectedProcess.ProcessName}, MainWindowTitle={report.SelectedProcess.MainWindowTitle}");
if (report.SelectedWindow is not null)
{
    Console.WriteLine($"Selected window: {report.SelectedWindow.HandleHex} {report.SelectedWindow.ClassName} {report.SelectedWindow.Title}");
}
return 0;

internal sealed record InspectorOptions(
    string ProcessPath,
    string ProcessName,
    string OutputPath,
    bool NormalizeWindowLayout,
    int NormalWindowX,
    int NormalWindowY,
    int NormalWindowWidth,
    int NormalWindowHeight)
{
    public static InspectorOptions Parse(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index += 2)
        {
            var key = args[index];
            if (!key.StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
            {
                continue;
            }

            values[key] = args[index + 1];
        }

        var processPath = values.TryGetValue("--process-path", out var configuredPath)
            ? configuredPath
            : @"C:\Program Files (x86)\Ecloud\CloudComputer\Ecloud Cloud Computer Application.exe";

        var processName = values.TryGetValue("--process-name", out var configuredName)
            ? configuredName
            : Path.GetFileNameWithoutExtension(processPath);

        var outputPath = values.TryGetValue("--output", out var configuredOutput)
            ? configuredOutput
            : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "samples", "ecloud-window-elements.md");

        var normalizeWindowLayout = ParseBool(values, "--normalize-window-layout", true);
        var normalWindowX = ParseInt(values, "--normal-window-x", 120);
        var normalWindowY = ParseInt(values, "--normal-window-y", 80);
        var normalWindowWidth = ParsePositiveInt(values, "--normal-window-width", 1600);
        var normalWindowHeight = ParsePositiveInt(values, "--normal-window-height", 900);

        return new InspectorOptions(
            Path.GetFullPath(processPath),
            processName,
            Path.GetFullPath(outputPath),
            normalizeWindowLayout,
            normalWindowX,
            normalWindowY,
            normalWindowWidth,
            normalWindowHeight);
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
    {
        return values.TryGetValue(key, out var configuredValue) && int.TryParse(configuredValue, out var parsedValue)
            ? parsedValue
            : fallback;
    }

    private static int ParsePositiveInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
    {
        var value = ParseInt(values, key, fallback);
        return value > 0 ? value : fallback;
    }

    private static bool ParseBool(IReadOnlyDictionary<string, string> values, string key, bool fallback)
    {
        if (!values.TryGetValue(key, out var configuredValue))
        {
            return fallback;
        }

        if (bool.TryParse(configuredValue, out var parsedBool))
        {
            return parsedBool;
        }

        if (int.TryParse(configuredValue, out var parsedInt))
        {
            return parsedInt != 0;
        }

        return fallback;
    }
}

internal static class WindowReportBuilder
{
    public static WindowReport Build(InspectorOptions options)
    {
        var candidates = Process.GetProcessesByName(options.ProcessName)
            .Select(ProcessSnapshot.Create)
            .Where(snapshot => snapshot.ExecutablePath is not null)
            .Where(snapshot => string.Equals(snapshot.ExecutablePath, options.ProcessPath, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(snapshot => snapshot.MainWindowHandle != 0)
            .ThenByDescending(snapshot => !string.IsNullOrWhiteSpace(snapshot.MainWindowTitle))
            .ThenByDescending(snapshot => snapshot.StartTime ?? DateTimeOffset.MinValue)
            .ToList();

        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"No running process matched '{options.ProcessPath}'.");
        }

        var selectedProcess = candidates[0];
        var initialTopLevelWindows = Win32WindowInspector.GetTopLevelWindows(selectedProcess.ProcessId);
        var initialSelectedWindow = ResolveSelectedWindow(selectedProcess, initialTopLevelWindows);
        var layoutAdjustment = options.NormalizeWindowLayout && initialSelectedWindow is not null
            ? Win32WindowInspector.NormalizeToRestoredLayout(
                initialSelectedWindow.Handle,
                options.NormalWindowX,
                options.NormalWindowY,
                options.NormalWindowWidth,
                options.NormalWindowHeight)
            : WindowLayoutAdjustment.NotRequested(options.NormalizeWindowLayout, initialSelectedWindow is not null);

        var topLevelWindows = Win32WindowInspector.GetTopLevelWindows(selectedProcess.ProcessId);
        var selectedWindow = ResolveSelectedWindow(selectedProcess, topLevelWindows) ?? initialSelectedWindow;
        var descendants = selectedWindow is null
            ? []
            : Win32WindowInspector.GetDescendantWindows(selectedWindow.Handle);

        var markdown = BuildMarkdown(options, selectedProcess, candidates, topLevelWindows, selectedWindow, descendants, layoutAdjustment);
        return new WindowReport(options.OutputPath, selectedProcess, selectedWindow, layoutAdjustment, markdown);
    }

    private static WindowElementSnapshot? ResolveSelectedWindow(ProcessSnapshot process, IReadOnlyList<WindowElementSnapshot> topLevelWindows)
    {
        if (process.MainWindowHandle != 0)
        {
            var exact = topLevelWindows.FirstOrDefault(window => window.Handle == process.MainWindowHandle);
            if (exact is not null)
            {
                return exact;
            }
        }

        return topLevelWindows
            .OrderByDescending(window => window.IsVisible)
            .ThenBy(window => window.IsMinimized)
            .ThenByDescending(window => window.Area)
            .FirstOrDefault();
    }

    private static string BuildMarkdown(
        InspectorOptions options,
        ProcessSnapshot selectedProcess,
        IReadOnlyList<ProcessSnapshot> processCandidates,
        IReadOnlyList<WindowElementSnapshot> topLevelWindows,
        WindowElementSnapshot? selectedWindow,
        IReadOnlyList<WindowElementSnapshot> descendants,
        WindowLayoutAdjustment layoutAdjustment)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Ecloud Window Element Snapshot");
        builder.AppendLine();
        builder.AppendLine($"- Captured at: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"- Target process path: `{options.ProcessPath}`");
        builder.AppendLine($"- Target process name: `{options.ProcessName}`");
        builder.AppendLine($"- Selected PID: `{selectedProcess.ProcessId}`");
        builder.AppendLine($"- Selected main window title: `{Escape(selectedProcess.MainWindowTitle)}`");
        builder.AppendLine($"- Selected main window handle: `{ToHandleHex(selectedProcess.MainWindowHandle)}`");
        builder.AppendLine();
        builder.AppendLine("## Process Candidates");
        builder.AppendLine();
        builder.AppendLine("| PID | Start Time | Main Window Handle | Main Window Title | Executable Path |");
        builder.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (var candidate in processCandidates)
        {
            builder.AppendLine($"| {candidate.ProcessId} | {FormatDate(candidate.StartTime)} | `{ToHandleHex(candidate.MainWindowHandle)}` | `{Escape(candidate.MainWindowTitle)}` | `{Escape(candidate.ExecutablePath)}` |");
        }

        builder.AppendLine();
        builder.AppendLine("## Top-level Windows");
        builder.AppendLine();
        builder.AppendLine("| Handle | Class | Title | Visible | Minimized | Enabled | Bounds | Area |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var window in topLevelWindows)
        {
            builder.AppendLine($"| `{window.HandleHex}` | `{Escape(window.ClassName)}` | `{Escape(window.Title)}` | {window.IsVisible} | {window.IsMinimized} | {window.IsEnabled} | `{FormatBounds(window.Bounds)}` | {window.Area} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Selected Main Window");
        builder.AppendLine();
        if (selectedWindow is null)
        {
            builder.AppendLine("No top-level window was selected.");
        }
        else
        {
            builder.AppendLine($"- Handle: `{selectedWindow.HandleHex}`");
            builder.AppendLine($"- Class: `{Escape(selectedWindow.ClassName)}`");
            builder.AppendLine($"- Title: `{Escape(selectedWindow.Title)}`");
            builder.AppendLine($"- Visible: `{selectedWindow.IsVisible}`");
            builder.AppendLine($"- Minimized: `{selectedWindow.IsMinimized}`");
            builder.AppendLine($"- Enabled: `{selectedWindow.IsEnabled}`");
            builder.AppendLine($"- Bounds: `{FormatBounds(selectedWindow.Bounds)}`");
            builder.AppendLine($"- Child/descendant count: `{descendants.Count}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Window Layout Adjustment (Before Probe)");
        builder.AppendLine();
        builder.AppendLine($"- Requested: `{layoutAdjustment.Requested}`");
        builder.AppendLine($"- Target normal bounds: `({options.NormalWindowX}, {options.NormalWindowY}) {options.NormalWindowWidth}x{options.NormalWindowHeight}`");
        builder.AppendLine($"- Was maximized: `{layoutAdjustment.WasMaximized}`");
        builder.AppendLine($"- Was minimized: `{layoutAdjustment.WasMinimized}`");
        builder.AppendLine($"- Restored to normal: `{layoutAdjustment.RestoredToNormal}`");
        builder.AppendLine($"- Resize applied: `{layoutAdjustment.ResizeSucceeded}`");
        builder.AppendLine($"- Message: `{Escape(layoutAdjustment.Message)}`");
        if (layoutAdjustment.BoundsBefore is not null)
        {
            builder.AppendLine($"- Bounds before: `{FormatBounds(layoutAdjustment.BoundsBefore)}`");
        }

        if (layoutAdjustment.BoundsAfter is not null)
        {
            builder.AppendLine($"- Bounds after: `{FormatBounds(layoutAdjustment.BoundsAfter)}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Descendant Window Elements");
        builder.AppendLine();
        if (descendants.Count == 0)
        {
            builder.AppendLine("No descendant HWND elements were found beneath the selected main window.");
        }
        else
        {
            builder.AppendLine("| Depth | Handle | Parent | Class | Title | Visible | Enabled | Bounds | Control ID |");
            builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
            foreach (var element in descendants.OrderBy(item => item.Depth).ThenBy(item => item.Handle))
            {
                builder.AppendLine($"| {element.Depth} | `{element.HandleHex}` | `{ToHandleHex(element.ParentHandle)}` | `{Escape(element.ClassName)}` | `{Escape(element.Title)}` | {element.IsVisible} | {element.IsEnabled} | `{FormatBounds(element.Bounds)}` | {element.ControlId} |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine();
        builder.AppendLine("- This snapshot uses Win32 window metadata only. It does not use OCR.");
        builder.AppendLine("- Chromium/Electron-based clients often expose only a shallow HWND tree; inner visual elements may be custom-drawn and therefore not visible as child windows.");
        builder.AppendLine("- If the selected main window is minimized, the descendant tree may be sparse even though the client is running normally.");

        return builder.ToString();
    }

    private static string Escape(string? value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("`", "'").Replace("|", "\\|");
    }

    private static string FormatDate(DateTimeOffset? value)
    {
        return value?.ToString("yyyy-MM-dd HH:mm:ss zzz") ?? string.Empty;
    }

    private static string FormatBounds(WindowBounds bounds)
    {
        return $"({bounds.Left}, {bounds.Top}) - ({bounds.Right}, {bounds.Bottom}) {bounds.Width}x{bounds.Height}";
    }

    private static string ToHandleHex(long value)
    {
        return value == 0 ? "0x0" : $"0x{value:X}";
    }
}

internal sealed record WindowReport(
    string OutputPath,
    ProcessSnapshot SelectedProcess,
    WindowElementSnapshot? SelectedWindow,
    WindowLayoutAdjustment LayoutAdjustment,
    string Markdown);

internal sealed record WindowLayoutAdjustment(
    bool Requested,
    bool WasMaximized,
    bool WasMinimized,
    bool RestoredToNormal,
    bool ResizeSucceeded,
    WindowBounds? BoundsBefore,
    WindowBounds? BoundsAfter,
    string Message)
{
    public static WindowLayoutAdjustment NotRequested(bool normalizationEnabled, bool hasWindow)
    {
        return !normalizationEnabled
            ? new WindowLayoutAdjustment(false, false, false, false, false, null, null, "Window layout normalization is disabled by option.")
            : new WindowLayoutAdjustment(true, false, false, false, false, null, null, hasWindow
                ? "Window layout normalization was requested but no actionable window was selected."
                : "No selected window is available for normalization.");
    }
}

internal sealed record ProcessSnapshot(
    int ProcessId,
    string ProcessName,
    string? ExecutablePath,
    DateTimeOffset? StartTime,
    long MainWindowHandle,
    string MainWindowTitle)
{
    public static ProcessSnapshot Create(Process process)
    {
        return new ProcessSnapshot(
            process.Id,
            process.ProcessName,
            TryGet(() => process.MainModule?.FileName),
            TryGet(() => new DateTimeOffset(process.StartTime)),
            process.MainWindowHandle.ToInt64(),
            TryGet(() => process.MainWindowTitle) ?? string.Empty);
    }

    private static T? TryGet<T>(Func<T?> accessor)
    {
        try
        {
            return accessor();
        }
        catch
        {
            return default;
        }
    }
}

internal sealed record WindowBounds(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

internal sealed record WindowElementSnapshot(
    long Handle,
    long ParentHandle,
    int Depth,
    string ClassName,
    string Title,
    bool IsVisible,
    bool IsEnabled,
    bool IsMinimized,
    WindowBounds Bounds,
    int ControlId)
{
    public string HandleHex => Handle == 0 ? "0x0" : $"0x{Handle:X}";
    public int Area => Math.Max(Bounds.Width, 0) * Math.Max(Bounds.Height, 0);
}

internal static class Win32WindowInspector
{
    public static WindowLayoutAdjustment NormalizeToRestoredLayout(long windowHandle, int x, int y, int width, int height)
    {
        if (windowHandle == 0)
        {
            return new WindowLayoutAdjustment(true, false, false, false, false, null, null, "Selected window handle is 0.");
        }

        var handle = new IntPtr(windowHandle);
        if (!IsWindow(handle))
        {
            return new WindowLayoutAdjustment(true, false, false, false, false, null, null, "Selected window is no longer valid.");
        }

        GetWindowRect(handle, out var beforeRect);
        var beforeBounds = new WindowBounds(beforeRect.Left, beforeRect.Top, beforeRect.Right, beforeRect.Bottom);
        var wasMaximized = IsZoomed(handle);
        var wasMinimized = IsIconic(handle);

        var restoreRequested = wasMaximized || wasMinimized;
        if (restoreRequested)
        {
            ShowWindow(handle, ShowWindowCommand.Restore);
        }

        var restoredToNormal = !IsZoomed(handle) && !IsIconic(handle);
        var resizeSucceeded = SetWindowPos(
            handle,
            IntPtr.Zero,
            x,
            y,
            width,
            height,
            SetWindowPosFlags.NoZOrder | SetWindowPosFlags.NoActivate | SetWindowPosFlags.ShowWindow);

        GetWindowRect(handle, out var afterRect);
        var afterBounds = new WindowBounds(afterRect.Left, afterRect.Top, afterRect.Right, afterRect.Bottom);
        var message = resizeSucceeded
            ? "Window was restored to normal state when needed and resized before detail probing."
            : "Window restore completed, but resizing failed.";

        return new WindowLayoutAdjustment(
            true,
            wasMaximized,
            wasMinimized,
            restoredToNormal,
            resizeSucceeded,
            beforeBounds,
            afterBounds,
            message);
    }

    public static IReadOnlyList<WindowElementSnapshot> GetTopLevelWindows(int processId)
    {
        var results = new List<WindowElementSnapshot>();
        var state = GCHandle.Alloc(new EnumerationState(processId, results));

        try
        {
            EnumWindows(static (handle, parameter) =>
            {
                var gcHandle = GCHandle.FromIntPtr(parameter);
                var enumerationState = (EnumerationState)gcHandle.Target!;
                GetWindowThreadProcessId(handle, out var currentProcessId);
                if (currentProcessId != enumerationState.ProcessId)
                {
                    return true;
                }

                enumerationState.Results.Add(CreateSnapshot(handle, 0, 0));
                return true;
            }, GCHandle.ToIntPtr(state));
        }
        finally
        {
            state.Free();
        }

        return results;
    }

    public static IReadOnlyList<WindowElementSnapshot> GetDescendantWindows(long parentHandle)
    {
        var results = new List<WindowElementSnapshot>();
        CollectChildren(new IntPtr(parentHandle), 1, results);
        return results;
    }

    private static void CollectChildren(IntPtr parentHandle, int depth, List<WindowElementSnapshot> results)
    {
        var childHandle = GetWindow(parentHandle, GetWindowCommand.FirstChild);
        while (childHandle != IntPtr.Zero)
        {
            results.Add(CreateSnapshot(childHandle, parentHandle.ToInt64(), depth));
            CollectChildren(childHandle, depth + 1, results);
            childHandle = GetWindow(childHandle, GetWindowCommand.NextSibling);
        }
    }

    private static WindowElementSnapshot CreateSnapshot(IntPtr handle, long parentHandle, int depth)
    {
        GetWindowRect(handle, out var rect);
        return new WindowElementSnapshot(
            handle.ToInt64(),
            parentHandle,
            depth,
            GetClassNameValue(handle),
            GetWindowTextValue(handle),
            IsWindowVisible(handle),
            IsWindowEnabled(handle),
            IsIconic(handle),
            new WindowBounds(rect.Left, rect.Top, rect.Right, rect.Bottom),
            GetDlgCtrlID(handle));
    }

    private sealed record EnumerationState(int ProcessId, List<WindowElementSnapshot> Results);

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr parameter);

    private enum GetWindowCommand : uint
    {
        FirstChild = 5,
        NextSibling = 2,
    }

    private enum ShowWindowCommand
    {
        Restore = 9,
    }

    [Flags]
    private enum SetWindowPosFlags : uint
    {
        NoZOrder = 0x0004,
        NoActivate = 0x0010,
        ShowWindow = 0x0040,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr handle, out Rect rect);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr handle, ShowWindowCommand command);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr handle,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        SetWindowPosFlags flags);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr handle, GetWindowCommand command);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr handle, StringBuilder text, int maxLength);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr handle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr handle, StringBuilder className, int maxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool IsWindowEnabled(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern int GetDlgCtrlID(IntPtr handle);

    private static string GetWindowTextValue(IntPtr handle)
    {
        var length = GetWindowTextLength(handle);
        if (length <= 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        GetWindowText(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    private static string GetClassNameValue(IntPtr handle)
    {
        var builder = new StringBuilder(256);
        GetClassName(handle, builder, builder.Capacity);
        return builder.ToString();
    }
}
