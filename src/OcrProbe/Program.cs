using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("OcrProbe only runs on Windows.");
    return 1;
}

var options = OcrProbeOptions.Parse(args);
var report = await OcrReportBuilder.BuildAsync(options);
Directory.CreateDirectory(Path.GetDirectoryName(report.OutputMarkdownPath)!);
Directory.CreateDirectory(Path.GetDirectoryName(report.OutputJsonPath)!);
Directory.CreateDirectory(Path.GetDirectoryName(report.CaptureImagePath)!);
await File.WriteAllTextAsync(report.OutputMarkdownPath, report.Markdown, new UTF8Encoding(false));
await File.WriteAllTextAsync(report.OutputJsonPath, JsonSerializer.Serialize(report.JsonSummary, new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true,
}), new UTF8Encoding(false));
report.CapturedBitmap.Save(report.CaptureImagePath, ImageFormat.Png);
Console.WriteLine($"OCR report written to {report.OutputMarkdownPath}");
Console.WriteLine($"OCR JSON written to {report.OutputJsonPath}");
Console.WriteLine($"Capture image written to {report.CaptureImagePath}");
Console.WriteLine($"Selected process: PID={report.SelectedProcess.ProcessId}, MainWindowTitle={report.SelectedProcess.MainWindowTitle}");
Console.WriteLine($"OCR text: {report.RecognizedText.Replace(Environment.NewLine, " ")}");
Console.WriteLine($"Matched keywords: {(report.MatchedKeywords.Count == 0 ? "<none>" : string.Join(", ", report.MatchedKeywords))}");
Console.WriteLine($"Any keyword matched: {report.AnyKeywordMatched}");
Console.WriteLine($"All keywords matched: {report.AllKeywordsMatched}");
Console.WriteLine($"Detected state: {report.DetectedState ?? "<none>"}");
return 0;

internal sealed record OcrProbeOptions(
    string ProcessPath,
    string ProcessName,
    string OutputMarkdownPath,
    string OutputJsonPath,
    string CaptureImagePath,
    CaptureMode CaptureMode,
    string LanguageTag,
    int RelativeX,
    int RelativeY,
    int RegionWidth,
    int RegionHeight,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<StateRule> StateRules)
{
    public static OcrProbeOptions Parse(string[] args)
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

        var outputMarkdownPath = values.TryGetValue("--output", out var configuredOutput)
            ? configuredOutput
            : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "samples", "ocr", "ecloud-ocr-probe.md");

        var captureImagePath = values.TryGetValue("--image-output", out var configuredImageOutput)
            ? configuredImageOutput
            : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "samples", "ocr", "ecloud-ocr-probe.png");

        var outputJsonPath = values.TryGetValue("--json-output", out var configuredJsonOutput)
            ? configuredJsonOutput
            : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "samples", "ocr", "ecloud-ocr-probe.json");

        var languageTag = values.TryGetValue("--language", out var configuredLanguage)
            ? configuredLanguage
            : "zh-CN";
        var captureMode = values.TryGetValue("--capture-mode", out var configuredCaptureMode) &&
            Enum.TryParse<CaptureMode>(configuredCaptureMode, true, out var parsedCaptureMode)
            ? parsedCaptureMode
            : CaptureMode.Window;

        var relativeX = ParseInt(values, "--relative-x", 120);
        var relativeY = ParseInt(values, "--relative-y", 220);
        var regionWidth = ParseInt(values, "--width", 760);
        var regionHeight = ParseInt(values, "--height", 520);
        var keywords = values.TryGetValue("--keywords", out var configuredKeywords)
            ? configuredKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : Array.Empty<string>();
        var stateRules = CreateDefaultStateRules();

        return new OcrProbeOptions(
            Path.GetFullPath(processPath),
            processName,
            Path.GetFullPath(outputMarkdownPath),
            Path.GetFullPath(outputJsonPath),
            Path.GetFullPath(captureImagePath),
            captureMode,
            languageTag,
            relativeX,
            relativeY,
            regionWidth,
            regionHeight,
            keywords,
            stateRules);
    }

    private static IReadOnlyList<StateRule> CreateDefaultStateRules()
    {
        return
        [
            new StateRule("Windows 已关机", ["Windows", "已关机"]),
            new StateRule("Windows 关机中", ["Windows", "关机中"]),
            new StateRule("Windows 运行中", ["Windows", "运行中"]),
        ];
    }

    private static int ParseInt(IReadOnlyDictionary<string, string> values, string key, int fallback)
    {
        return values.TryGetValue(key, out var configuredValue) && int.TryParse(configuredValue, out var parsedValue)
            ? parsedValue
            : fallback;
    }
}

internal static class OcrReportBuilder
{
    public static async Task<OcrReport> BuildAsync(OcrProbeOptions options)
    {
        var processes = Process.GetProcessesByName(options.ProcessName)
            .Select(ProcessSnapshot.Create)
            .Where(snapshot => snapshot.ExecutablePath is not null)
            .Where(snapshot => string.Equals(snapshot.ExecutablePath, options.ProcessPath, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(snapshot => snapshot.MainWindowHandle != 0)
            .ThenByDescending(snapshot => !string.IsNullOrWhiteSpace(snapshot.MainWindowTitle))
            .ThenByDescending(snapshot => snapshot.StartTime ?? DateTimeOffset.MinValue)
            .ToList();

        if (processes.Count == 0)
        {
            throw new InvalidOperationException($"No running process matched '{options.ProcessPath}'.");
        }

        var selectedProcess = processes[0];
        if (selectedProcess.MainWindowHandle == 0)
        {
            throw new InvalidOperationException("The selected process does not have a main window handle.");
        }

        var window = Win32Capture.GetWindowInfo(selectedProcess.MainWindowHandle);
        if (!window.IsVisible)
        {
            throw new InvalidOperationException("The selected main window is not visible. OCR probe requires a visible window.");
        }

        if (window.IsMinimized)
        {
            throw new InvalidOperationException("The selected main window is minimized. Restore it before running the OCR probe.");
        }

        var captureRegion = Win32Capture.ResolveCaptureRegion(window.ClientBounds, options.RelativeX, options.RelativeY, options.RegionWidth, options.RegionHeight);
        using var bitmap = Win32Capture.CaptureRegion(window, captureRegion, options.CaptureMode);
        var recognized = await RecognizeAsync(bitmap, options.LanguageTag);
        var normalizedRecognizedText = NormalizeForKeywordMatch(recognized.Text);
        var matchedKeywords = options.Keywords
            .Where(keyword => normalizedRecognizedText.Contains(NormalizeForKeywordMatch(keyword), StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var anyKeywordMatched = matchedKeywords.Count > 0;
        var allKeywordsMatched = options.Keywords.Count == 0 || matchedKeywords.Count == options.Keywords.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var detectedState = DetectState(options.StateRules, normalizedRecognizedText);

        var markdown = BuildMarkdown(options, selectedProcess, window, captureRegion, recognized, matchedKeywords, normalizedRecognizedText, anyKeywordMatched, allKeywordsMatched, detectedState);
        var jsonSummary = new OcrJsonSummary(
            DateTimeOffset.Now,
            options.ProcessPath,
            selectedProcess.ProcessId,
            selectedProcess.MainWindowTitle,
            ToHandleHex(selectedProcess.MainWindowHandle),
            options.LanguageTag,
            new OcrWindowSummary(window.ClassName, window.IsVisible, window.IsMinimized, window.WindowBounds, window.ClientBounds),
            new OcrCaptureSummary(options.CaptureMode, options.RelativeX, options.RelativeY, options.RegionWidth, options.RegionHeight, captureRegion.AbsoluteBounds),
            recognized.Text,
            normalizedRecognizedText,
            matchedKeywords,
            anyKeywordMatched,
            allKeywordsMatched,
            detectedState,
            options.StateRules,
            recognized.Lines);

        return new OcrReport(options.OutputMarkdownPath, options.OutputJsonPath, options.CaptureImagePath, selectedProcess, bitmap.Clone(new Rectangle(0, 0, bitmap.Width, bitmap.Height), bitmap.PixelFormat), markdown, recognized.Text, matchedKeywords, anyKeywordMatched, allKeywordsMatched, detectedState, jsonSummary);
    }

    private static string? DetectState(IReadOnlyList<StateRule> stateRules, string normalizedRecognizedText)
    {
        foreach (var stateRule in stateRules)
        {
            var allTermsPresent = stateRule.RequiredTerms
                .Select(NormalizeForKeywordMatch)
                .All(term => normalizedRecognizedText.Contains(term, StringComparison.OrdinalIgnoreCase));

            if (allTermsPresent)
            {
                return stateRule.Name;
            }
        }

        return null;
    }

    private static string NormalizeForKeywordMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString();
    }

    private static async Task<OcrRecognitionSnapshot> RecognizeAsync(Bitmap bitmap, string languageTag)
    {
        var engine = TryCreateEngine(languageTag);
        if (engine is null)
        {
            var availableLanguages = string.Join(", ", OcrEngine.AvailableRecognizerLanguages.Select(language => language.LanguageTag));
            throw new InvalidOperationException($"No OCR engine was available for '{languageTag}'. Available recognizer languages: {availableLanguages}");
        }

        var softwareBitmap = await ConvertToSoftwareBitmapAsync(bitmap);
        var result = await engine.RecognizeAsync(softwareBitmap);

        var lines = result.Lines.Select(line => new OcrLineSnapshot(
            line.Text,
            CreateLineBounds(line))).ToList();

        return new OcrRecognitionSnapshot(result.Text, lines);
    }

    private static OcrRectangleSnapshot CreateLineBounds(OcrLine line)
    {
        if (line.Words.Count == 0)
        {
            return new OcrRectangleSnapshot(0, 0, 0, 0);
        }

        var left = line.Words.Min(word => word.BoundingRect.X);
        var top = line.Words.Min(word => word.BoundingRect.Y);
        var right = line.Words.Max(word => word.BoundingRect.X + word.BoundingRect.Width);
        var bottom = line.Words.Max(word => word.BoundingRect.Y + word.BoundingRect.Height);
        return new OcrRectangleSnapshot(
            Convert.ToUInt32(Math.Max(left, 0)),
            Convert.ToUInt32(Math.Max(top, 0)),
            Convert.ToUInt32(Math.Max(right - left, 0)),
            Convert.ToUInt32(Math.Max(bottom - top, 0)));
    }

    private static OcrEngine? TryCreateEngine(string languageTag)
    {
        OcrEngine? engine = null;
        try
        {
            engine = OcrEngine.TryCreateFromLanguage(new Language(languageTag));
        }
        catch
        {
        }

        return engine ?? OcrEngine.TryCreateFromUserProfileLanguages();
    }

    private static async Task<SoftwareBitmap> ConvertToSoftwareBitmapAsync(Bitmap bitmap)
    {
        using var memoryStream = new MemoryStream();
        bitmap.Save(memoryStream, ImageFormat.Png);
        memoryStream.Position = 0;
        var bytes = memoryStream.ToArray();

        using var randomAccessStream = new InMemoryRandomAccessStream();
        using (var outputStream = randomAccessStream.GetOutputStreamAt(0))
        using (var writer = new DataWriter(outputStream))
        {
            writer.WriteBytes(bytes);
            await writer.StoreAsync();
            await writer.FlushAsync();
        }

        randomAccessStream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream);
        return await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
    }

    private static string BuildMarkdown(
        OcrProbeOptions options,
        ProcessSnapshot selectedProcess,
        WindowInfo window,
        CaptureRegion captureRegion,
        OcrRecognitionSnapshot recognition,
        IReadOnlyList<string> matchedKeywords,
        string normalizedRecognizedText,
        bool anyKeywordMatched,
        bool allKeywordsMatched,
        string? detectedState)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Ecloud OCR Probe");
        builder.AppendLine();
        builder.AppendLine($"- Captured at: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"- Target process path: `{options.ProcessPath}`");
        builder.AppendLine($"- Selected PID: `{selectedProcess.ProcessId}`");
        builder.AppendLine($"- Selected main window title: `{Escape(selectedProcess.MainWindowTitle)}`");
        builder.AppendLine($"- Selected main window handle: `{ToHandleHex(selectedProcess.MainWindowHandle)}`");
        builder.AppendLine($"- OCR language: `{options.LanguageTag}`");
        builder.AppendLine($"- Capture mode: `{options.CaptureMode}`");
        builder.AppendLine($"- Capture image: `{options.CaptureImagePath}`");
        builder.AppendLine();
        builder.AppendLine("## Window Info");
        builder.AppendLine();
        builder.AppendLine($"- Window class: `{Escape(window.ClassName)}`");
        builder.AppendLine($"- Window visible: `{window.IsVisible}`");
        builder.AppendLine($"- Window minimized: `{window.IsMinimized}`");
        builder.AppendLine($"- Window bounds: `{FormatRect(window.WindowBounds)}`");
        builder.AppendLine($"- Client bounds: `{FormatRect(window.ClientBounds)}`");
        builder.AppendLine();
        builder.AppendLine("## Capture Region");
        builder.AppendLine();
        builder.AppendLine($"- Relative offset: `({options.RelativeX}, {options.RelativeY})`");
        builder.AppendLine($"- Relative size: `{options.RegionWidth}x{options.RegionHeight}`");
        builder.AppendLine($"- Absolute capture rect: `{FormatRect(captureRegion.AbsoluteBounds)}`");
        builder.AppendLine();
        builder.AppendLine("## OCR Result");
        builder.AppendLine();
        builder.AppendLine("```text");
        builder.AppendLine(string.IsNullOrWhiteSpace(recognition.Text) ? "<empty>" : recognition.Text.TrimEnd());
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("Normalized OCR text:");
        builder.AppendLine("```text");
        builder.AppendLine(string.IsNullOrWhiteSpace(normalizedRecognizedText) ? "<empty>" : normalizedRecognizedText);
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## Keyword Match");
        builder.AppendLine();
        builder.AppendLine($"- Configured keywords: `{string.Join(", ", options.Keywords)}`");
        builder.AppendLine($"- Matched keywords: `{(matchedKeywords.Count == 0 ? "<none>" : string.Join(", ", matchedKeywords))}`");
        builder.AppendLine($"- Any keyword matched: `{anyKeywordMatched}`");
        builder.AppendLine($"- All keywords matched: `{allKeywordsMatched}`");
        builder.AppendLine($"- Page hit by custom keywords: `{allKeywordsMatched}`");
        builder.AppendLine($"- Detected state: `{detectedState ?? "<none>"}`");
        builder.AppendLine($"- State hit: `{(detectedState is not null)}`");
        builder.AppendLine();
        builder.AppendLine("Configured states:");
        foreach (var stateRule in options.StateRules)
        {
            builder.AppendLine($"- `{stateRule.Name}` requires `{string.Join(" + ", stateRule.RequiredTerms)}`");
        }
        builder.AppendLine();
        builder.AppendLine("## OCR Lines");
        builder.AppendLine();
        if (recognition.Lines.Count == 0)
        {
            builder.AppendLine("No OCR text lines were recognized in the selected region.");
        }
        else
        {
            builder.AppendLine("| Text | Bounds |");
            builder.AppendLine("| --- | --- |");
            foreach (var line in recognition.Lines)
            {
                builder.AppendLine($"| `{Escape(line.Text)}` | `{line.Bounds.X},{line.Bounds.Y},{line.Bounds.Width},{line.Bounds.Height}` |");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Notes");
        builder.AppendLine();
        builder.AppendLine("- This probe is designed for fixed-region OCR only.");
        builder.AppendLine("- It captures the screen pixels from the selected visible window client area.");
        builder.AppendLine("- It does not use OCR for full-page understanding; it only checks whether configured keywords appear in the selected region.");
        return builder.ToString();
    }

    private static string Escape(string? value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : value.Replace("`", "'").Replace("|", "\\|");
    }

    private static string FormatRect(Int32Rect rect)
    {
        return $"({rect.X}, {rect.Y}) {rect.Width}x{rect.Height}";
    }

    private static string ToHandleHex(long value)
    {
        return value == 0 ? "0x0" : $"0x{value:X}";
    }
}

internal sealed record OcrReport(
    string OutputMarkdownPath,
    string OutputJsonPath,
    string CaptureImagePath,
    ProcessSnapshot SelectedProcess,
    Bitmap CapturedBitmap,
    string Markdown,
    string RecognizedText,
    IReadOnlyList<string> MatchedKeywords,
    bool AnyKeywordMatched,
    bool AllKeywordsMatched,
    string? DetectedState,
    OcrJsonSummary JsonSummary);

internal sealed record StateRule(string Name, IReadOnlyList<string> RequiredTerms);

internal sealed record OcrJsonSummary(
    DateTimeOffset CapturedAt,
    string TargetProcessPath,
    int SelectedProcessId,
    string SelectedMainWindowTitle,
    string SelectedMainWindowHandle,
    string OcrLanguage,
    OcrWindowSummary Window,
    OcrCaptureSummary Capture,
    string RecognizedText,
    string NormalizedRecognizedText,
    IReadOnlyList<string> MatchedKeywords,
    bool AnyKeywordMatched,
    bool AllKeywordsMatched,
    string? DetectedState,
    IReadOnlyList<StateRule> ConfiguredStates,
    IReadOnlyList<OcrLineSnapshot> Lines);

internal sealed record OcrWindowSummary(
    string ClassName,
    bool IsVisible,
    bool IsMinimized,
    Int32Rect WindowBounds,
    Int32Rect ClientBounds);

internal sealed record OcrCaptureSummary(
    CaptureMode CaptureMode,
    int RelativeX,
    int RelativeY,
    int Width,
    int Height,
    Int32Rect AbsoluteBounds);

internal enum CaptureMode
{
    Screen,
    Window,
}

internal sealed record OcrRecognitionSnapshot(string Text, IReadOnlyList<OcrLineSnapshot> Lines);

internal sealed record OcrLineSnapshot(string Text, OcrRectangleSnapshot Bounds);

internal sealed record OcrRectangleSnapshot(uint X, uint Y, uint Width, uint Height);

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

internal sealed record Int32Rect(int X, int Y, int Width, int Height);

internal sealed record WindowInfo(long Handle, string ClassName, bool IsVisible, bool IsMinimized, Int32Rect WindowBounds, Int32Rect ClientBounds);

internal sealed record CaptureRegion(Int32Rect AbsoluteBounds, Int32Rect RelativeBounds);

internal static class Win32Capture
{
    public static WindowInfo GetWindowInfo(long handleValue)
    {
        var handle = new IntPtr(handleValue);
        if (!GetWindowRect(handle, out var windowRect))
        {
            throw new InvalidOperationException("Failed to read the window bounds.");
        }

        if (!GetClientRect(handle, out var clientRect))
        {
            throw new InvalidOperationException("Failed to read the client bounds.");
        }

        var topLeft = new PointStruct { X = clientRect.Left, Y = clientRect.Top };
        var bottomRight = new PointStruct { X = clientRect.Right, Y = clientRect.Bottom };
        if (!ClientToScreen(handle, ref topLeft) || !ClientToScreen(handle, ref bottomRight))
        {
            throw new InvalidOperationException("Failed to translate client bounds into screen coordinates.");
        }

        return new WindowInfo(
            handleValue,
            GetClassName(handle),
            IsWindowVisible(handle),
            IsIconic(handle),
            new Int32Rect(windowRect.Left, windowRect.Top, windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top),
            new Int32Rect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y));
    }

    public static CaptureRegion ResolveCaptureRegion(Int32Rect clientBounds, int relativeX, int relativeY, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new InvalidOperationException("The OCR capture region must have positive width and height.");
        }

        var absoluteX = clientBounds.X + relativeX;
        var absoluteY = clientBounds.Y + relativeY;
        if (absoluteX < clientBounds.X || absoluteY < clientBounds.Y || absoluteX + width > clientBounds.X + clientBounds.Width || absoluteY + height > clientBounds.Y + clientBounds.Height)
        {
            throw new InvalidOperationException("The OCR capture region is outside the selected window client bounds.");
        }

        return new CaptureRegion(
            new Int32Rect(absoluteX, absoluteY, width, height),
            new Int32Rect(relativeX, relativeY, width, height));
    }

    public static Bitmap CaptureRegion(WindowInfo window, CaptureRegion region, CaptureMode captureMode)
    {
        if (captureMode == CaptureMode.Window)
        {
            return CaptureWindowRegion(window, region);
        }

        return CaptureScreenRegion(region);
    }

    private static Bitmap CaptureWindowRegion(WindowInfo window, CaptureRegion region)
    {
        using var clientBitmap = CaptureClientBitmap(window);
        var relative = region.RelativeBounds;
        var croppedBitmap = new Bitmap(relative.Width, relative.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(croppedBitmap);
        graphics.DrawImage(
            clientBitmap,
            new Rectangle(0, 0, relative.Width, relative.Height),
            new Rectangle(relative.X, relative.Y, relative.Width, relative.Height),
            GraphicsUnit.Pixel);

        return croppedBitmap;
    }

    private static Bitmap CaptureClientBitmap(WindowInfo window)
    {
        var windowBitmap = new Bitmap(window.WindowBounds.Width, window.WindowBounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(windowBitmap))
        {
            var hdc = graphics.GetHdc();
            try
            {
                if (!PrintWindow(new IntPtr(window.Handle), hdc, PrintWindowFlags.RenderFullContent))
                {
                    graphics.ReleaseHdc(hdc);
                    windowBitmap.Dispose();
                    return CaptureScreenRegion(new CaptureRegion(window.ClientBounds, new Int32Rect(0, 0, window.ClientBounds.Width, window.ClientBounds.Height)));
                }
            }
            finally
            {
                graphics.ReleaseHdc(hdc);
            }
        }

        var offsetX = window.ClientBounds.X - window.WindowBounds.X;
        var offsetY = window.ClientBounds.Y - window.WindowBounds.Y;
        var clientBitmap = new Bitmap(window.ClientBounds.Width, window.ClientBounds.Height, PixelFormat.Format32bppArgb);
        using var clientGraphics = Graphics.FromImage(clientBitmap);
        clientGraphics.DrawImage(
            windowBitmap,
            new Rectangle(0, 0, clientBitmap.Width, clientBitmap.Height),
            new Rectangle(offsetX, offsetY, clientBitmap.Width, clientBitmap.Height),
            GraphicsUnit.Pixel);
        windowBitmap.Dispose();
        return clientBitmap;
    }

    private static Bitmap CaptureScreenRegion(CaptureRegion region)
    {
        var bitmap = new Bitmap(region.AbsoluteBounds.Width, region.AbsoluteBounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.AbsoluteBounds.X, region.AbsoluteBounds.Y, 0, 0, new Size(region.AbsoluteBounds.Width, region.AbsoluteBounds.Height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static string GetClassName(IntPtr handle)
    {
        var builder = new StringBuilder(256);
        GetClassName(handle, builder, builder.Capacity);
        return builder.ToString();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RectStruct
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointStruct
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr handle, out RectStruct rect);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr handle, out RectStruct rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr handle, ref PointStruct point);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr handle, IntPtr deviceContext, PrintWindowFlags flags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr handle, StringBuilder className, int maxCount);

    [Flags]
    private enum PrintWindowFlags : uint
    {
        None = 0x0,
        RenderFullContent = 0x2,
    }
}
