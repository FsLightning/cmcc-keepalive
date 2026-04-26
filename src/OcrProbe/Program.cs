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
    Console.Error.WriteLine("OcrProbe 仅支持在 Windows 上运行。");
    return 1;
}

var options = OcrProbeOptions.Parse(args);
var report = await OcrReportBuilder.BuildAsync(options);
Directory.CreateDirectory(Path.GetDirectoryName(report.OutputMarkdownPath)!);
Directory.CreateDirectory(Path.GetDirectoryName(report.OutputJsonPath)!);
Directory.CreateDirectory(Path.GetDirectoryName(report.CaptureImagePath)!);
await File.WriteAllTextAsync(report.OutputMarkdownPath, report.Markdown, new UTF8Encoding(false));
await File.WriteAllTextAsync(
    report.OutputJsonPath,
    JsonSerializer.Serialize(
        report.JsonSummary,
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        }),
    new UTF8Encoding(false));
report.CapturedBitmap.Save(report.CaptureImagePath, ImageFormat.Png);

Console.WriteLine($"OCR Markdown 报告已写入: {report.OutputMarkdownPath}");
Console.WriteLine($"OCR JSON 结果已写入: {report.OutputJsonPath}");
Console.WriteLine($"截图文件已写入: {report.CaptureImagePath}");
Console.WriteLine($"选中进程: PID={report.Process.ProcessId}, MainWindowTitle={report.Process.MainWindowTitle}");
Console.WriteLine($"OCR 文本: {report.JsonSummary.Text.Original.Replace(Environment.NewLine, " ")}");
Console.WriteLine($"匹配关键词: {(report.JsonSummary.CustomKeywordDetection.MatchedKeywords.Count == 0 ? "<none>" : string.Join(", ", report.JsonSummary.CustomKeywordDetection.MatchedKeywords))}");
Console.WriteLine($"任意关键词命中: {report.JsonSummary.CustomKeywordDetection.AnyMatched}");
Console.WriteLine($"全部关键词命中: {report.JsonSummary.CustomKeywordDetection.AllMatched}");
Console.WriteLine($"识别状态: {report.JsonSummary.StateDetection.DetectedState ?? "<none>"}");
return 0;

internal sealed record OcrProbeOptions(
    string ProcessPath,
    string ProcessName,
    string OutputMarkdownPath,
    string OutputJsonPath,
    string CaptureImagePath,
    CaptureMode CaptureMode,
    string LanguageTag,
    bool NormalizeWindowLayout,
    int NormalWindowX,
    int NormalWindowY,
    int NormalWindowWidth,
    int NormalWindowHeight,
    bool UseAdaptiveRegion,
    int BaselineClientWidth,
    int BaselineClientHeight,
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
        var outputJsonPath = values.TryGetValue("--json-output", out var configuredJsonOutput)
            ? configuredJsonOutput
            : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "samples", "ocr", "ecloud-ocr-probe.json");
        var captureImagePath = values.TryGetValue("--image-output", out var configuredImageOutput)
            ? configuredImageOutput
            : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "docs", "samples", "ocr", "ecloud-ocr-probe.png");
        var languageTag = values.TryGetValue("--language", out var configuredLanguage)
            ? configuredLanguage
            : "zh-CN";
        var captureMode = values.TryGetValue("--capture-mode", out var configuredCaptureMode) &&
            Enum.TryParse<CaptureMode>(configuredCaptureMode, true, out var parsedCaptureMode)
            ? parsedCaptureMode
            : CaptureMode.Window;
        var normalizeWindowLayout = ParseBool(values, "--normalize-window-layout", false);
        var normalWindowX = ParseInt(values, "--normal-window-x", 120);
        var normalWindowY = ParseInt(values, "--normal-window-y", 80);
        var normalWindowWidth = ParsePositiveInt(values, "--normal-window-width", 1600);
        var normalWindowHeight = ParsePositiveInt(values, "--normal-window-height", 900);
        var useAdaptiveRegion = ParseBool(values, "--adaptive-region", true);
        var baselineClientWidth = ParsePositiveInt(values, "--baseline-client-width", 2032);
        var baselineClientHeight = ParsePositiveInt(values, "--baseline-client-height", 1074);

        return new OcrProbeOptions(
            Path.GetFullPath(processPath),
            processName,
            Path.GetFullPath(outputMarkdownPath),
            Path.GetFullPath(outputJsonPath),
            Path.GetFullPath(captureImagePath),
            captureMode,
            languageTag,
            normalizeWindowLayout,
            normalWindowX,
            normalWindowY,
            normalWindowWidth,
            normalWindowHeight,
            useAdaptiveRegion,
            baselineClientWidth,
            baselineClientHeight,
            ParseInt(values, "--relative-x", 120),
            ParseInt(values, "--relative-y", 220),
            ParseInt(values, "--width", 1200),
            ParseInt(values, "--height", 700),
            ParseKeywords(values),
            CreateDefaultStateRules());
    }

    private static IReadOnlyList<string> ParseKeywords(IReadOnlyDictionary<string, string> values)
    {
        return values.TryGetValue("--keywords", out var configuredKeywords)
            ? configuredKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : [];
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

internal static class OcrReportBuilder
{
    public static async Task<OcrReport> BuildAsync(OcrProbeOptions options)
    {
        var processCandidates = Process.GetProcessesByName(options.ProcessName)
            .Select(ProcessSnapshot.Create)
            .Where(snapshot => snapshot.ExecutablePath is not null)
            .Where(snapshot => string.Equals(snapshot.ExecutablePath, options.ProcessPath, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(snapshot => snapshot.MainWindowHandle != 0)
            .ThenByDescending(snapshot => !string.IsNullOrWhiteSpace(snapshot.MainWindowTitle))
            .ThenByDescending(snapshot => snapshot.StartTime ?? DateTimeOffset.MinValue)
            .ToList();

        if (processCandidates.Count == 0)
        {
            throw new InvalidOperationException($"未找到匹配进程: {options.ProcessPath}");
        }

        var selectedProcess = processCandidates[0];
        var layoutAdjustment = options.NormalizeWindowLayout
            ? Win32Capture.NormalizeToRestoredLayout(
                selectedProcess.MainWindowHandle,
                options.NormalWindowX,
                options.NormalWindowY,
                options.NormalWindowWidth,
                options.NormalWindowHeight)
            : WindowLayoutAdjustment.NotRequested();

        selectedProcess = TryRefreshProcessSnapshot(selectedProcess.ProcessId) ?? selectedProcess;
        var initialCandidates = CaptureWindowSelector.GetOrderedCandidates(selectedProcess, options);
        if (initialCandidates.Count == 0)
        {
            throw new InvalidOperationException("未找到可用于 OCR 的窗口候选。请先确认客户端窗口已经创建。");
        }

        var initialEvaluation = await EvaluateCaptureCandidatesAsync(initialCandidates, options, "initial");
        OcrCaptureAttempt? bestAttempt = initialEvaluation.BestAttempt;
        var attemptSummaries = new List<CaptureAttemptSummary>(initialEvaluation.Attempts);

        if (ShouldRetryWithNormalization(options, bestAttempt, selectedProcess))
        {
            layoutAdjustment = Win32Capture.NormalizeToRestoredLayout(
                selectedProcess.MainWindowHandle,
                options.NormalWindowX,
                options.NormalWindowY,
                options.NormalWindowWidth,
                options.NormalWindowHeight);
            selectedProcess = TryRefreshProcessSnapshot(selectedProcess.ProcessId) ?? selectedProcess;
            var retryCandidates = CaptureWindowSelector.GetOrderedCandidates(selectedProcess, options);
            if (retryCandidates.Count > 0)
            {
                var retryEvaluation = await EvaluateCaptureCandidatesAsync(retryCandidates, options, "normalized-retry");
                attemptSummaries.AddRange(retryEvaluation.Attempts);
                if (retryEvaluation.BestAttempt is not null)
                {
                    if (bestAttempt is null || retryEvaluation.BestAttempt.Score > bestAttempt.Score)
                    {
                        bestAttempt?.CapturedBitmap.Dispose();
                        bestAttempt = retryEvaluation.BestAttempt;
                    }
                    else
                    {
                        retryEvaluation.BestAttempt.CapturedBitmap.Dispose();
                    }
                }
            }
        }

        if (bestAttempt is null)
        {
            throw new InvalidOperationException("所有窗口候选的 OCR 抓图都失败了。请检查窗口是否已创建，或尝试切换 capture mode。");
        }

        var jsonSummary = BuildJsonSummary(options, selectedProcess, layoutAdjustment, bestAttempt, attemptSummaries);
        var markdown = BuildMarkdownReport(options, jsonSummary);
        return new OcrReport(
            options.OutputMarkdownPath,
            options.OutputJsonPath,
            options.CaptureImagePath,
            bestAttempt.CapturedBitmap,
            markdown,
            jsonSummary);
    }

    private static async Task<CaptureEvaluation> EvaluateCaptureCandidatesAsync(
        IReadOnlyList<CaptureWindowCandidate> captureCandidates,
        OcrProbeOptions options,
        string stageTag)
    {
        OcrCaptureAttempt? bestAttempt = null;
        var attemptSummaries = new List<CaptureAttemptSummary>();

        foreach (var candidate in captureCandidates)
        {
            var captureRegions = Win32Capture.ResolveCaptureRegions(candidate.ClientBounds, options);
            if (captureRegions.Count == 0)
            {
                attemptSummaries.Add(new CaptureAttemptSummary(
                    candidate.HandleHex,
                    candidate.SourceKind,
                    $"{stageTag}/<none>",
                    candidate.Title,
                    candidate.ClassName,
                    candidate.IsVisible,
                    candidate.IsMinimized,
                    false,
                    false,
                    0,
                    null,
                    $"[{stageTag}] 没有可用于当前窗口尺寸的捕获区域。"));
                continue;
            }

            foreach (var captureRegion in captureRegions)
            {
                using var bitmap = Win32Capture.TryCaptureRegion(candidate, captureRegion, options.CaptureMode);
                var regionStrategy = $"{stageTag}/{captureRegion.Strategy}";
                if (bitmap is null)
                {
                    attemptSummaries.Add(new CaptureAttemptSummary(
                        candidate.HandleHex,
                        candidate.SourceKind,
                        regionStrategy,
                        candidate.Title,
                        candidate.ClassName,
                        candidate.IsVisible,
                        candidate.IsMinimized,
                        true,
                        false,
                        0,
                        null,
                        $"[{regionStrategy}] 窗口抓图失败。"));
                    continue;
                }

                var recognizedText = await RecognizeAsync(bitmap, options.LanguageTag);
                var customKeywordDetection = EvaluateCustomKeywords(options.Keywords, recognizedText.Normalized);
                var stateDetection = EvaluateStates(options.StateRules, recognizedText.Normalized);
                var attemptResult = string.IsNullOrWhiteSpace(recognizedText.Normalized)
                    ? $"[{regionStrategy}] 抓图与 OCR 成功，但目标区域没有识别到文本。"
                    : $"[{regionStrategy}] 抓图与 OCR 成功。";
                var attempt = new OcrCaptureAttempt(
                    candidate,
                    captureRegion,
                    (Bitmap)bitmap.Clone(),
                    recognizedText,
                    customKeywordDetection,
                    stateDetection);

                attemptSummaries.Add(new CaptureAttemptSummary(
                    candidate.HandleHex,
                    candidate.SourceKind,
                    regionStrategy,
                    candidate.Title,
                    candidate.ClassName,
                    candidate.IsVisible,
                    candidate.IsMinimized,
                    true,
                    true,
                    recognizedText.Normalized.Length,
                    stateDetection.DetectedState,
                    attemptResult));

                if (bestAttempt is null || attempt.Score > bestAttempt.Score)
                {
                    bestAttempt?.CapturedBitmap.Dispose();
                    bestAttempt = attempt;
                }
                else
                {
                    attempt.CapturedBitmap.Dispose();
                }
            }
        }

        return new CaptureEvaluation(bestAttempt, attemptSummaries);
    }

    private static bool ShouldRetryWithNormalization(OcrProbeOptions options, OcrCaptureAttempt? bestAttempt, ProcessSnapshot selectedProcess)
    {
        if (options.NormalizeWindowLayout)
        {
            return false;
        }

        if (selectedProcess.MainWindowHandle == 0)
        {
            return false;
        }

        if (bestAttempt is null)
        {
            return true;
        }

        if (bestAttempt.StateDetection.IsMatched)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(bestAttempt.Text.Normalized);
    }

    private static ProcessSnapshot? TryRefreshProcessSnapshot(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return ProcessSnapshot.Create(process);
        }
        catch
        {
            return null;
        }
    }

    private static CustomKeywordDetectionSummary EvaluateCustomKeywords(IReadOnlyList<string> keywords, string normalizedRecognizedText)
    {
        var configuredKeywords = keywords
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var matchedKeywords = configuredKeywords
            .Where(keyword => normalizedRecognizedText.Contains(NormalizeForMatch(keyword), StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new CustomKeywordDetectionSummary(
            configuredKeywords,
            matchedKeywords,
            matchedKeywords.Count > 0,
            configuredKeywords.Count > 0 && matchedKeywords.Count == configuredKeywords.Count);
    }

    private static StateDetectionSummary EvaluateStates(IReadOnlyList<StateRule> stateRules, string normalizedRecognizedText)
    {
        var evaluations = new List<StateRuleEvaluation>(stateRules.Count);
        string? detectedState = null;

        foreach (var stateRule in stateRules)
        {
            var matchedTerms = new List<string>(stateRule.RequiredTerms.Count);
            var missingTerms = new List<string>(stateRule.RequiredTerms.Count);

            foreach (var requiredTerm in stateRule.RequiredTerms)
            {
                if (normalizedRecognizedText.Contains(NormalizeForMatch(requiredTerm), StringComparison.OrdinalIgnoreCase))
                {
                    matchedTerms.Add(requiredTerm);
                }
                else
                {
                    missingTerms.Add(requiredTerm);
                }
            }

            var isMatched = missingTerms.Count == 0;
            if (detectedState is null && isMatched)
            {
                detectedState = stateRule.Name;
            }

            evaluations.Add(new StateRuleEvaluation(
                stateRule.Name,
                stateRule.RequiredTerms,
                matchedTerms,
                missingTerms,
                isMatched));
        }

        return new StateDetectionSummary(detectedState, detectedState is not null, evaluations);
    }

    private static async Task<OcrTextSummary> RecognizeAsync(Bitmap bitmap, string languageTag)
    {
        var engine = TryCreateEngine(languageTag);
        if (engine is null)
        {
            var availableLanguages = string.Join(", ", OcrEngine.AvailableRecognizerLanguages.Select(language => language.LanguageTag));
            throw new InvalidOperationException($"无法创建 OCR 引擎。可用语言: {availableLanguages}");
        }

        var softwareBitmap = await ConvertToSoftwareBitmapAsync(bitmap);
        var result = await engine.RecognizeAsync(softwareBitmap);
        var lines = result.Lines.Select(line => new OcrLineSnapshot(line.Text, CreateLineBounds(line))).ToList();

        return new OcrTextSummary(
            result.Text,
            NormalizeForMatch(result.Text),
            lines);
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

    private static string NormalizeForMatch(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (!char.IsWhiteSpace(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static OcrJsonSummary BuildJsonSummary(
        OcrProbeOptions options,
        ProcessSnapshot selectedProcess,
        WindowLayoutAdjustment layoutAdjustment,
        OcrCaptureAttempt bestAttempt,
        IReadOnlyList<CaptureAttemptSummary> attemptSummaries)
    {
        return new OcrJsonSummary(
            "1.2",
            DateTimeOffset.Now,
            options.ProcessPath,
            new ProcessSummary(
                selectedProcess.ProcessId,
                selectedProcess.ProcessName,
                selectedProcess.ExecutablePath,
                selectedProcess.MainWindowTitle,
                ToHandleHex(selectedProcess.MainWindowHandle)),
            layoutAdjustment,
            new CaptureWindowSummary(
                bestAttempt.Candidate.HandleHex,
                bestAttempt.Candidate.Title,
                bestAttempt.Candidate.ClassName,
                bestAttempt.Candidate.SourceKind,
                bestAttempt.Candidate.SelectionReason,
                bestAttempt.Candidate.IsVisible,
                bestAttempt.Candidate.IsMinimized,
                bestAttempt.Candidate.IsEnabled,
                bestAttempt.Candidate.WindowBounds,
                bestAttempt.Candidate.ClientBounds,
                bestAttempt.Candidate.SelectionScore),
            new CaptureRegionSummary(
                options.CaptureMode,
                bestAttempt.CaptureRegion.Strategy,
                bestAttempt.CaptureRegion.RelativeBounds,
                bestAttempt.CaptureRegion.AbsoluteBounds),
            bestAttempt.Text,
            bestAttempt.CustomKeywordDetection,
            bestAttempt.StateDetection,
            attemptSummaries);
    }

    private static string BuildMarkdownReport(OcrProbeOptions options, OcrJsonSummary summary)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Ecloud OCR 探测报告");
        builder.AppendLine();
        builder.AppendLine($"- 采集时间: {summary.CapturedAt:yyyy-MM-dd HH:mm:ss zzz}");
        builder.AppendLine($"- 目标进程路径: `{summary.TargetProcessPath}`");
        builder.AppendLine($"- 目标进程 PID: `{summary.Process.ProcessId}`");
        builder.AppendLine($"- 目标主窗口标题: `{Escape(summary.Process.MainWindowTitle)}`");
        builder.AppendLine($"- OCR 语言: `{options.LanguageTag}`");
        builder.AppendLine($"- 抓图模式: `{summary.Capture.CaptureMode}`");
        builder.AppendLine();
        builder.AppendLine("## 预处理窗口布局");
        builder.AppendLine();
        builder.AppendLine($"- 已请求: `{summary.LayoutAdjustment.Requested}`");
        builder.AppendLine($"- 目标普通窗体尺寸: `({options.NormalWindowX}, {options.NormalWindowY}) {options.NormalWindowWidth}x{options.NormalWindowHeight}`");
        builder.AppendLine($"- 预处理前是否最大化: `{summary.LayoutAdjustment.WasMaximized}`");
        builder.AppendLine($"- 预处理前是否最小化: `{summary.LayoutAdjustment.WasMinimized}`");
        builder.AppendLine($"- 已恢复为普通窗体: `{summary.LayoutAdjustment.RestoredToNormal}`");
        builder.AppendLine($"- 已应用目标尺寸: `{summary.LayoutAdjustment.ResizeSucceeded}`");
        builder.AppendLine($"- 结果说明: `{Escape(summary.LayoutAdjustment.Message)}`");
        if (summary.LayoutAdjustment.BoundsBefore is not null)
        {
            builder.AppendLine($"- 预处理前区域: `{FormatRect(summary.LayoutAdjustment.BoundsBefore)}`");
        }

        if (summary.LayoutAdjustment.BoundsAfter is not null)
        {
            builder.AppendLine($"- 预处理后区域: `{FormatRect(summary.LayoutAdjustment.BoundsAfter)}`");
        }

        builder.AppendLine();
        builder.AppendLine("## 选中捕获窗口");
        builder.AppendLine();
        builder.AppendLine($"- 句柄: `{summary.SelectedCaptureWindow.HandleHex}`");
        builder.AppendLine($"- 标题: `{Escape(summary.SelectedCaptureWindow.Title)}`");
        builder.AppendLine($"- 类名: `{Escape(summary.SelectedCaptureWindow.ClassName)}`");
        builder.AppendLine($"- 来源: `{summary.SelectedCaptureWindow.SourceKind}`");
        builder.AppendLine($"- 选择原因: `{Escape(summary.SelectedCaptureWindow.SelectionReason)}`");
        builder.AppendLine($"- 可见: `{summary.SelectedCaptureWindow.IsVisible}`");
        builder.AppendLine($"- 最小化: `{summary.SelectedCaptureWindow.IsMinimized}`");
        builder.AppendLine($"- 启用: `{summary.SelectedCaptureWindow.IsEnabled}`");
        builder.AppendLine($"- 窗口区域: `{FormatRect(summary.SelectedCaptureWindow.WindowBounds)}`");
        builder.AppendLine($"- Client 区域: `{FormatRect(summary.SelectedCaptureWindow.ClientBounds)}`");
        builder.AppendLine();
        builder.AppendLine("## 默认状态区域");
        builder.AppendLine();
        builder.AppendLine($"- 区域策略: `{summary.Capture.Strategy}`");
        builder.AppendLine($"- 相对区域: `{FormatRect(summary.Capture.RelativeBounds)}`");
        builder.AppendLine($"- 绝对区域: `{FormatRect(summary.Capture.AbsoluteBounds)}`");
        builder.AppendLine("- 说明: `默认区域已经固化为中部偏左状态检测区域。`");
        builder.AppendLine();
        builder.AppendLine("## OCR 文本");
        builder.AppendLine();
        builder.AppendLine("```text");
        builder.AppendLine(string.IsNullOrWhiteSpace(summary.Text.Original) ? "<empty>" : summary.Text.Original.TrimEnd());
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("归一化文本:");
        builder.AppendLine("```text");
        builder.AppendLine(string.IsNullOrWhiteSpace(summary.Text.Normalized) ? "<empty>" : summary.Text.Normalized);
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("## 自定义关键词结果");
        builder.AppendLine();
        builder.AppendLine($"- 配置关键词: `{string.Join(", ", summary.CustomKeywordDetection.ConfiguredKeywords)}`");
        builder.AppendLine($"- 命中关键词: `{(summary.CustomKeywordDetection.MatchedKeywords.Count == 0 ? "<none>" : string.Join(", ", summary.CustomKeywordDetection.MatchedKeywords))}`");
        builder.AppendLine($"- 任意命中: `{summary.CustomKeywordDetection.AnyMatched}`");
        builder.AppendLine($"- 全部命中: `{summary.CustomKeywordDetection.AllMatched}`");
        builder.AppendLine();
        builder.AppendLine("## 三态识别结果");
        builder.AppendLine();
        builder.AppendLine($"- 当前识别状态: `{summary.StateDetection.DetectedState ?? "<none>"}`");
        builder.AppendLine($"- 状态命中: `{summary.StateDetection.IsMatched}`");
        builder.AppendLine();
        builder.AppendLine("状态规则:");
        foreach (var state in summary.StateDetection.Candidates)
        {
            builder.AppendLine($"- `{state.Name}`: 命中=`{state.IsMatched}`，已满足=`{string.Join(", ", state.MatchedTerms)}`，缺失=`{string.Join(", ", state.MissingTerms)}`");
        }
        builder.AppendLine();
        builder.AppendLine("## OCR 行明细");
        builder.AppendLine();
        if (summary.Text.Lines.Count == 0)
        {
            builder.AppendLine("当前区域没有识别出任何文本行。");
        }
        else
        {
            builder.AppendLine("| 文本 | 边界 |");
            builder.AppendLine("| --- | --- |");
            foreach (var line in summary.Text.Lines)
            {
                builder.AppendLine($"| `{Escape(line.Text)}` | `{line.Bounds.X},{line.Bounds.Y},{line.Bounds.Width},{line.Bounds.Height}` |");
            }
        }
        builder.AppendLine();
        builder.AppendLine("## 窗口候选尝试");
        builder.AppendLine();
        builder.AppendLine("| 句柄 | 来源 | 区域策略 | 标题 | 类名 | 可见 | 最小化 | 区域可用 | 抓图成功 | 识别状态 | 文本长度 | 结果 |");
        builder.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var attempt in summary.AttemptedWindows)
        {
            builder.AppendLine($"| `{attempt.HandleHex}` | `{attempt.SourceKind}` | `{attempt.RegionStrategy}` | `{Escape(attempt.Title)}` | `{Escape(attempt.ClassName)}` | {attempt.IsVisible} | {attempt.IsMinimized} | {attempt.RegionFits} | {attempt.CaptureSucceeded} | `{attempt.DetectedState ?? "<none>"}` | {attempt.NormalizedTextLength} | `{Escape(attempt.Result)}` |");
        }
        builder.AppendLine();
        builder.AppendLine("## 说明");
        builder.AppendLine();
        builder.AppendLine("- 该工具只做固定区域 OCR 与只读状态识别。");
        builder.AppendLine("- `window` 模式会优先尝试通过窗口句柄抓图，以减少前台遮挡的影响。");
        builder.AppendLine("- `screen` 模式仍然依赖目标窗口实际显示在屏幕上。");
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

internal static class CaptureWindowSelector
{
    public static IReadOnlyList<CaptureWindowCandidate> GetOrderedCandidates(ProcessSnapshot process, OcrProbeOptions options)
    {
        var seeds = new Dictionary<long, CandidateSeed>();

        AddSeed(process.MainWindowHandle, "MainWindow", "来自进程 MainWindowHandle。", 300);
        foreach (var handle in Win32Capture.GetTopLevelWindowHandles(process.ProcessId))
        {
            AddSeed(handle, "TopLevelWindow", "来自同进程顶层窗口。", 200);
        }

        if (process.MainWindowHandle != 0)
        {
            foreach (var handle in Win32Capture.GetDescendantWindowHandles(process.MainWindowHandle, maxDepth: 2))
            {
                AddSeed(handle, "RenderHost", "来自主窗口下的子窗口或 RenderHost。", 260);
            }
        }

        var candidates = new List<CaptureWindowCandidate>(seeds.Count);
        foreach (var seed in seeds.Values)
        {
            if (!Win32Capture.TryGetWindowInfo(seed.Handle, out var window))
            {
                continue;
            }

            if (IsIgnoredWindow(window))
            {
                continue;
            }

            var fitsRequestedRegion = window.ClientBounds.Width >= options.RelativeX + options.RegionWidth &&
                window.ClientBounds.Height >= options.RelativeY + options.RegionHeight;
            var scoreReasons = new List<string>();
            var selectionScore = seed.BaseScore;

            if (window.IsVisible)
            {
                selectionScore += 120;
                scoreReasons.Add("可见");
            }
            else
            {
                selectionScore -= 220;
                scoreReasons.Add("不可见");
            }

            if (!window.IsMinimized)
            {
                selectionScore += 100;
                scoreReasons.Add("未最小化");
            }
            else
            {
                selectionScore -= 180;
                scoreReasons.Add("最小化");
            }

            if (window.IsEnabled)
            {
                selectionScore += 40;
            }

            if (fitsRequestedRegion)
            {
                selectionScore += 150;
                scoreReasons.Add("可容纳默认状态区域");
            }
            else
            {
                selectionScore -= 150;
                scoreReasons.Add("Client 区域不足");
            }

            if (!string.IsNullOrWhiteSpace(window.Title))
            {
                selectionScore += 30;
            }

            if (window.Title.Contains("移动云电脑", StringComparison.OrdinalIgnoreCase))
            {
                selectionScore += 80;
                scoreReasons.Add("标题命中移动云电脑");
            }

            if (window.Title.Contains("settingWindow", StringComparison.OrdinalIgnoreCase))
            {
                selectionScore -= 250;
                scoreReasons.Add("疑似设置窗口");
            }

            if (window.ClassName.Contains("Chrome_RenderWidgetHostHWND", StringComparison.OrdinalIgnoreCase))
            {
                selectionScore += 80;
                scoreReasons.Add("RenderHost 内容窗口");
            }
            else if (window.ClassName.Contains("Chrome_WidgetWin_0", StringComparison.OrdinalIgnoreCase))
            {
                selectionScore += 20;
            }

            var areaBonus = Math.Min((window.ClientBounds.Width * window.ClientBounds.Height) / 20000, 120);
            selectionScore += areaBonus;

            if (IsOffscreenWindow(window))
            {
                selectionScore -= 320;
                scoreReasons.Add("离屏窗口");
            }

            candidates.Add(new CaptureWindowCandidate(
                window.Handle,
                window.HandleHex,
                window.Title,
                window.ClassName,
                seed.SourceKind,
                seed.SourceReason,
                string.Join("，", scoreReasons),
                window.IsVisible,
                window.IsMinimized,
                window.IsEnabled,
                window.WindowBounds,
                window.ClientBounds,
                fitsRequestedRegion,
                selectionScore));
        }

        return candidates
            .OrderByDescending(candidate => candidate.SelectionScore)
            .ThenByDescending(candidate => candidate.IsVisible)
            .ThenBy(candidate => candidate.IsMinimized)
            .ThenByDescending(candidate => candidate.ClientBounds.Width * candidate.ClientBounds.Height)
            .ToList();

        void AddSeed(long handle, string sourceKind, string sourceReason, int baseScore)
        {
            if (handle == 0)
            {
                return;
            }

            if (seeds.TryGetValue(handle, out var existing) && existing.BaseScore >= baseScore)
            {
                return;
            }

            seeds[handle] = new CandidateSeed(handle, sourceKind, sourceReason, baseScore);
        }
    }

    private static bool IsIgnoredWindow(WindowInfo window)
    {
        if (window.ClassName.Contains("Electron_NotifyIconHostWindow", StringComparison.OrdinalIgnoreCase) ||
            window.ClassName.Contains("Base_PowerMessageWindow", StringComparison.OrdinalIgnoreCase) ||
            window.ClassName.Contains("Chrome_SystemMessageWindow", StringComparison.OrdinalIgnoreCase) ||
            window.ClassName.Contains("MSCTFIME UI", StringComparison.OrdinalIgnoreCase) ||
            window.ClassName.Equals("IME", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return window.Title.Contains("settingWindow", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOffscreenWindow(WindowInfo window)
    {
        return window.WindowBounds.X <= -3000 ||
            window.WindowBounds.Y <= -3000 ||
            window.ClientBounds.X <= -3000 ||
            window.ClientBounds.Y <= -3000;
    }
}

internal static class Win32Capture
{
    public static WindowLayoutAdjustment NormalizeToRestoredLayout(long windowHandle, int x, int y, int width, int height)
    {
        if (windowHandle == 0)
        {
            return new WindowLayoutAdjustment(true, false, false, false, false, null, null, "主窗口句柄为空，无法预处理。");
        }

        var handle = new IntPtr(windowHandle);
        if (!IsWindow(handle))
        {
            return new WindowLayoutAdjustment(true, false, false, false, false, null, null, "主窗口句柄无效，无法预处理。");
        }

        if (!GetWindowRect(handle, out var beforeRect))
        {
            return new WindowLayoutAdjustment(true, false, false, false, false, null, null, "读取预处理前窗口区域失败。");
        }

        var wasMaximized = IsZoomed(handle);
        var wasMinimized = IsIconic(handle);
        if (wasMaximized || wasMinimized)
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
        var beforeBounds = new Int32Rect(beforeRect.Left, beforeRect.Top, beforeRect.Right - beforeRect.Left, beforeRect.Bottom - beforeRect.Top);
        var afterBounds = new Int32Rect(afterRect.Left, afterRect.Top, afterRect.Right - afterRect.Left, afterRect.Bottom - afterRect.Top);
        var message = resizeSucceeded
            ? "OCR 前已尝试恢复普通窗体并调整到目标分辨率。"
            : "窗口已恢复普通状态，但调整分辨率失败。";

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

    public static bool TryGetWindowInfo(long handleValue, out WindowInfo window)
    {
        window = default!;

        var handle = new IntPtr(handleValue);
        if (!GetWindowRect(handle, out var windowRect) || !GetClientRect(handle, out var clientRect))
        {
            return false;
        }

        var topLeft = new PointStruct { X = clientRect.Left, Y = clientRect.Top };
        var bottomRight = new PointStruct { X = clientRect.Right, Y = clientRect.Bottom };
        if (!ClientToScreen(handle, ref topLeft) || !ClientToScreen(handle, ref bottomRight))
        {
            return false;
        }

        window = new WindowInfo(
            handleValue,
            ToHandleHex(handleValue),
            GetWindowTextValue(handle),
            GetClassNameValue(handle),
            IsWindowVisible(handle),
            IsIconic(handle),
            IsWindowEnabled(handle),
            new Int32Rect(windowRect.Left, windowRect.Top, windowRect.Right - windowRect.Left, windowRect.Bottom - windowRect.Top),
            new Int32Rect(topLeft.X, topLeft.Y, bottomRight.X - topLeft.X, bottomRight.Y - topLeft.Y));
        return true;
    }

    public static IReadOnlyList<long> GetTopLevelWindowHandles(int processId)
    {
        var handles = new List<long>();
        EnumWindows((handle, parameter) =>
        {
            GetWindowThreadProcessId(handle, out var currentProcessId);
            if (currentProcessId == processId)
            {
                handles.Add(handle.ToInt64());
            }

            return true;
        }, IntPtr.Zero);
        return handles;
    }

    public static IReadOnlyList<long> GetDescendantWindowHandles(long parentHandle, int maxDepth)
    {
        var handles = new List<long>();
        CollectChildren(new IntPtr(parentHandle), 1, maxDepth, handles);
        return handles;
    }

    public static IReadOnlyList<CaptureRegion> ResolveCaptureRegions(Int32Rect clientBounds, OcrProbeOptions options)
    {
        var regions = new List<CaptureRegion>();
        var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        TryAddRegion("fixed", options.RelativeX, options.RelativeY, options.RegionWidth, options.RegionHeight);

        if (options.UseAdaptiveRegion)
        {
            var scaleX = clientBounds.Width / (double)options.BaselineClientWidth;
            var scaleY = clientBounds.Height / (double)options.BaselineClientHeight;
            var scaledX = Math.Max(0, (int)Math.Round(options.RelativeX * scaleX));
            var scaledY = Math.Max(0, (int)Math.Round(options.RelativeY * scaleY));
            var scaledWidth = Math.Max(1, (int)Math.Round(options.RegionWidth * scaleX));
            var scaledHeight = Math.Max(1, (int)Math.Round(options.RegionHeight * scaleY));
            TryAddRegion("adaptive-scaled", scaledX, scaledY, scaledWidth, scaledHeight);
        }

        var clampedX = Math.Clamp(options.RelativeX, 0, Math.Max(clientBounds.Width - 1, 0));
        var clampedY = Math.Clamp(options.RelativeY, 0, Math.Max(clientBounds.Height - 1, 0));
        var clampedWidth = Math.Min(options.RegionWidth, Math.Max(clientBounds.Width - clampedX, 0));
        var clampedHeight = Math.Min(options.RegionHeight, Math.Max(clientBounds.Height - clampedY, 0));
        if (clampedWidth >= 240 && clampedHeight >= 160)
        {
            TryAddRegion("fit-to-client", clampedX, clampedY, clampedWidth, clampedHeight);
        }

        return regions;

        void TryAddRegion(string strategy, int relativeX, int relativeY, int width, int height)
        {
            if (!TryCreateRegion(clientBounds, relativeX, relativeY, width, height, strategy, out var region))
            {
                return;
            }

            var key = $"{region.RelativeBounds.X}:{region.RelativeBounds.Y}:{region.RelativeBounds.Width}:{region.RelativeBounds.Height}";
            if (!dedupe.Add(key))
            {
                return;
            }

            regions.Add(region);
        }
    }

    private static bool TryCreateRegion(Int32Rect clientBounds, int relativeX, int relativeY, int width, int height, string strategy, out CaptureRegion captureRegion)
    {
        captureRegion = default!;
        if (clientBounds.Width <= 0 || clientBounds.Height <= 0 || width <= 0 || height <= 0)
        {
            return false;
        }

        if (relativeX < 0 || relativeY < 0)
        {
            return false;
        }

        if (relativeX + width > clientBounds.Width || relativeY + height > clientBounds.Height)
        {
            return false;
        }

        var absoluteX = clientBounds.X + relativeX;
        var absoluteY = clientBounds.Y + relativeY;
        captureRegion = new CaptureRegion(
            new Int32Rect(absoluteX, absoluteY, width, height),
            new Int32Rect(relativeX, relativeY, width, height),
            strategy);
        return true;
    }

    public static Bitmap? TryCaptureRegion(CaptureWindowCandidate candidate, CaptureRegion region, CaptureMode captureMode)
    {
        return captureMode == CaptureMode.Window
            ? TryCaptureWindowRegion(candidate, region)
            : TryCaptureScreenRegion(candidate, region);
    }

    // window 模式优先走句柄抓图，避免前台窗口遮挡导致读到错误内容。
    private static Bitmap? TryCaptureWindowRegion(CaptureWindowCandidate candidate, CaptureRegion region)
    {
        using var clientBitmap = TryCaptureClientBitmap(candidate);
        if (clientBitmap is null)
        {
            return candidate.IsVisible && !candidate.IsMinimized
                ? TryCaptureScreenRegion(candidate, region)
                : null;
        }

        var relative = region.RelativeBounds;
        if (relative.X < 0 || relative.Y < 0 || relative.X + relative.Width > clientBitmap.Width || relative.Y + relative.Height > clientBitmap.Height)
        {
            return null;
        }

        var croppedBitmap = new Bitmap(relative.Width, relative.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(croppedBitmap);
        graphics.DrawImage(
            clientBitmap,
            new Rectangle(0, 0, relative.Width, relative.Height),
            new Rectangle(relative.X, relative.Y, relative.Width, relative.Height),
            GraphicsUnit.Pixel);
        return croppedBitmap;
    }

    private static Bitmap? TryCaptureClientBitmap(CaptureWindowCandidate candidate)
    {
        if (candidate.WindowBounds.Width <= 0 || candidate.WindowBounds.Height <= 0 || candidate.ClientBounds.Width <= 0 || candidate.ClientBounds.Height <= 0)
        {
            return null;
        }

        using var windowBitmap = new Bitmap(candidate.WindowBounds.Width, candidate.WindowBounds.Height, PixelFormat.Format32bppArgb);
        var printed = false;
        using (var windowGraphics = Graphics.FromImage(windowBitmap))
        {
            var hdc = windowGraphics.GetHdc();
            try
            {
                printed = PrintWindow(new IntPtr(candidate.Handle), hdc, PrintWindowFlags.RenderFullContent);
            }
            finally
            {
                windowGraphics.ReleaseHdc(hdc);
            }
        }

        if (!printed)
        {
            return null;
        }

        var offsetX = candidate.ClientBounds.X - candidate.WindowBounds.X;
        var offsetY = candidate.ClientBounds.Y - candidate.WindowBounds.Y;
        if (offsetX < 0 || offsetY < 0 || offsetX + candidate.ClientBounds.Width > windowBitmap.Width || offsetY + candidate.ClientBounds.Height > windowBitmap.Height)
        {
            return null;
        }

        var clientBitmap = new Bitmap(candidate.ClientBounds.Width, candidate.ClientBounds.Height, PixelFormat.Format32bppArgb);
        using var clientGraphics = Graphics.FromImage(clientBitmap);
        clientGraphics.DrawImage(
            windowBitmap,
            new Rectangle(0, 0, clientBitmap.Width, clientBitmap.Height),
            new Rectangle(offsetX, offsetY, clientBitmap.Width, clientBitmap.Height),
            GraphicsUnit.Pixel);
        return clientBitmap;
    }

    private static Bitmap? TryCaptureScreenRegion(CaptureWindowCandidate candidate, CaptureRegion region)
    {
        if (!candidate.IsVisible || candidate.IsMinimized)
        {
            return null;
        }

        var bitmap = new Bitmap(region.AbsoluteBounds.Width, region.AbsoluteBounds.Height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(region.AbsoluteBounds.X, region.AbsoluteBounds.Y, 0, 0, new Size(region.AbsoluteBounds.Width, region.AbsoluteBounds.Height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static void CollectChildren(IntPtr parentHandle, int depth, int maxDepth, List<long> handles)
    {
        if (depth > maxDepth)
        {
            return;
        }

        var childHandle = GetWindow(parentHandle, GetWindowCommand.FirstChild);
        while (childHandle != IntPtr.Zero)
        {
            handles.Add(childHandle.ToInt64());
            CollectChildren(childHandle, depth + 1, maxDepth, handles);
            childHandle = GetWindow(childHandle, GetWindowCommand.NextSibling);
        }
    }

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

    private static string ToHandleHex(long value)
    {
        return value == 0 ? "0x0" : $"0x{value:X}";
    }

    private delegate bool EnumWindowsProc(IntPtr handle, IntPtr parameter);

    private enum GetWindowCommand : uint
    {
        NextSibling = 2,
        FirstChild = 5,
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
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr handle, out int processId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr handle, out RectStruct rect);

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
    private static extern bool GetClientRect(IntPtr handle, out RectStruct rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr handle, ref PointStruct point);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern bool IsWindowEnabled(IntPtr handle);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr handle, GetWindowCommand command);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr handle, IntPtr deviceContext, PrintWindowFlags flags);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr handle);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr handle, StringBuilder text, int maxLength);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr handle, StringBuilder className, int maxCount);

    [Flags]
    private enum PrintWindowFlags : uint
    {
        RenderFullContent = 0x2,
    }
}

internal sealed record OcrReport(
    string OutputMarkdownPath,
    string OutputJsonPath,
    string CaptureImagePath,
    Bitmap CapturedBitmap,
    string Markdown,
    OcrJsonSummary JsonSummary)
{
    public ProcessSummary Process => JsonSummary.Process;
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

internal sealed record CandidateSeed(long Handle, string SourceKind, string SourceReason, int BaseScore);

internal sealed record CaptureEvaluation(
    OcrCaptureAttempt? BestAttempt,
    IReadOnlyList<CaptureAttemptSummary> Attempts);

internal sealed record CaptureWindowCandidate(
    long Handle,
    string HandleHex,
    string Title,
    string ClassName,
    string SourceKind,
    string SourceReason,
    string SelectionReason,
    bool IsVisible,
    bool IsMinimized,
    bool IsEnabled,
    Int32Rect WindowBounds,
    Int32Rect ClientBounds,
    bool FitsRequestedRegion,
    int SelectionScore);

internal sealed record OcrCaptureAttempt(
    CaptureWindowCandidate Candidate,
    CaptureRegion CaptureRegion,
    Bitmap CapturedBitmap,
    OcrTextSummary Text,
    CustomKeywordDetectionSummary CustomKeywordDetection,
    StateDetectionSummary StateDetection)
{
    public int Score =>
        (StateDetection.IsMatched ? 100_000 : 0) +
        (CustomKeywordDetection.AnyMatched ? 10_000 : 0) +
        GetRegionBonus(CaptureRegion.Strategy) +
        Text.Normalized.Length +
        Candidate.SelectionScore;

    private static int GetRegionBonus(string strategy)
    {
        return strategy switch
        {
            "adaptive-scaled" => 100,
            "fixed" => 60,
            "fit-to-client" => 20,
            _ => 0,
        };
    }
}

internal sealed record OcrJsonSummary(
    string SchemaVersion,
    DateTimeOffset CapturedAt,
    string TargetProcessPath,
    ProcessSummary Process,
    WindowLayoutAdjustment LayoutAdjustment,
    CaptureWindowSummary SelectedCaptureWindow,
    CaptureRegionSummary Capture,
    OcrTextSummary Text,
    CustomKeywordDetectionSummary CustomKeywordDetection,
    StateDetectionSummary StateDetection,
    IReadOnlyList<CaptureAttemptSummary> AttemptedWindows);

internal sealed record WindowLayoutAdjustment(
    bool Requested,
    bool WasMaximized,
    bool WasMinimized,
    bool RestoredToNormal,
    bool ResizeSucceeded,
    Int32Rect? BoundsBefore,
    Int32Rect? BoundsAfter,
    string Message)
{
    public static WindowLayoutAdjustment NotRequested()
    {
        return new WindowLayoutAdjustment(
            false,
            false,
            false,
            false,
            false,
            null,
            null,
            "窗口预处理已关闭。"
        );
    }
}

internal sealed record ProcessSummary(
    int ProcessId,
    string ProcessName,
    string? ExecutablePath,
    string MainWindowTitle,
    string MainWindowHandleHex);

internal sealed record CaptureWindowSummary(
    string HandleHex,
    string Title,
    string ClassName,
    string SourceKind,
    string SelectionReason,
    bool IsVisible,
    bool IsMinimized,
    bool IsEnabled,
    Int32Rect WindowBounds,
    Int32Rect ClientBounds,
    int SelectionScore);

internal sealed record CaptureRegionSummary(
    CaptureMode CaptureMode,
    string Strategy,
    Int32Rect RelativeBounds,
    Int32Rect AbsoluteBounds);

internal sealed record CustomKeywordDetectionSummary(
    IReadOnlyList<string> ConfiguredKeywords,
    IReadOnlyList<string> MatchedKeywords,
    bool AnyMatched,
    bool AllMatched);

internal sealed record StateDetectionSummary(
    string? DetectedState,
    bool IsMatched,
    IReadOnlyList<StateRuleEvaluation> Candidates);

internal sealed record StateRule(string Name, IReadOnlyList<string> RequiredTerms);

internal sealed record StateRuleEvaluation(
    string Name,
    IReadOnlyList<string> RequiredTerms,
    IReadOnlyList<string> MatchedTerms,
    IReadOnlyList<string> MissingTerms,
    bool IsMatched);

internal sealed record OcrTextSummary(
    string Original,
    string Normalized,
    IReadOnlyList<OcrLineSnapshot> Lines);

internal sealed record CaptureAttemptSummary(
    string HandleHex,
    string SourceKind,
    string RegionStrategy,
    string Title,
    string ClassName,
    bool IsVisible,
    bool IsMinimized,
    bool RegionFits,
    bool CaptureSucceeded,
    int NormalizedTextLength,
    string? DetectedState,
    string Result);

internal enum CaptureMode
{
    Screen,
    Window,
}

internal sealed record OcrLineSnapshot(string Text, OcrRectangleSnapshot Bounds);

internal sealed record OcrRectangleSnapshot(uint X, uint Y, uint Width, uint Height);

internal sealed record Int32Rect(int X, int Y, int Width, int Height);

internal sealed record WindowInfo(
    long Handle,
    string HandleHex,
    string Title,
    string ClassName,
    bool IsVisible,
    bool IsMinimized,
    bool IsEnabled,
    Int32Rect WindowBounds,
    Int32Rect ClientBounds);

internal sealed record CaptureRegion(Int32Rect AbsoluteBounds, Int32Rect RelativeBounds, string Strategy);
