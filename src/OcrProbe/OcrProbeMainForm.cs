using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

internal sealed class OcrProbeMainForm : Form
{
    private readonly string[] _launchArgs;
    private readonly TextBox _processPathTextBox = new() { Dock = DockStyle.Fill };
    private readonly TextBox _keywordsTextBox = new() { Dock = DockStyle.Fill };
    private readonly ComboBox _captureModeComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130 };
    private readonly CheckBox _performClickCheckBox = new() { Text = "执行点击", AutoSize = true };
    private readonly CheckBox _normalizeLayoutCheckBox = new() { Text = "窗口标准化", AutoSize = true };
    private readonly CheckBox _autoStartCcAppCheckBox = new() { Text = "自动启动 CCApp", AutoSize = true, Checked = true };
    private readonly NumericUpDown _clickDelayNumeric = new() { Minimum = 0, Maximum = 120, Width = 72, Value = 1 };
    private readonly NumericUpDown _startupWaitNumeric = new() { Minimum = 1, Maximum = 120, Increment = 1, Value = 12, Width = 72 };
    private readonly NumericUpDown _postStartDelayNumeric = new() { Minimum = 0, Maximum = 120, Increment = 1, Value = 5, Width = 72 };
    private readonly NumericUpDown _postEnterDelayNumeric = new() { Minimum = 0, Maximum = 120, Increment = 1, Value = 3, Width = 72 };
    private readonly Button _browseProcessButton = new() { Text = "浏览...", AutoSize = true };
    private readonly Button _runButton = new() { Text = "执行", AutoSize = true };
    private readonly Button _openMarkdownButton = new() { Text = "打开 Markdown", AutoSize = true, Enabled = false };
    private readonly Button _openJsonButton = new() { Text = "打开 JSON", AutoSize = true, Enabled = false };
    private readonly Button _openImageButton = new() { Text = "打开截图", AutoSize = true, Enabled = false };
    private readonly Label _statusLabel = new() { Text = "空闲", AutoSize = true };
    private readonly TextBox _resultTextBox = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
        Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point),
    };

    private OcrReport? _lastReport;
    private readonly IReadOnlyList<CaptureModeOption> _captureModeOptions =
    [
        new CaptureModeOption(CaptureMode.Window, "窗口"),
        new CaptureModeOption(CaptureMode.Screen, "屏幕"),
    ];
    private readonly string _uiStateFilePath =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "cmcc-keepalive",
            "ocrprobe-ui-state.json");

    public OcrProbeMainForm(string[] launchArgs)
    {
        _launchArgs = launchArgs;
        Text = "OcrProbe 简易界面";
        Width = 1100;
        Height = 760;
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout();
        InitializeDefaults();
        TryLoadUiState();
        WireEvents();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(10),
        };

        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(CreateProcessRow(), 0, 0);
        root.Controls.Add(CreateKeywordsRow(), 0, 1);
        root.Controls.Add(CreateOptionsRow(), 0, 2);
        root.Controls.Add(CreateActionRow(), 0, 3);
        root.Controls.Add(_resultTextBox, 0, 4);

        Controls.Add(root);
    }

    private Control CreateProcessRow()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        panel.Controls.Add(new Label { Text = "进程路径", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        panel.Controls.Add(_processPathTextBox, 1, 0);
        panel.Controls.Add(_browseProcessButton, 2, 0);
        panel.Controls.Add(_statusLabel, 3, 0);
        return panel;
    }

    private Control CreateKeywordsRow()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        };

        panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label { Text = "关键词(逗号分隔)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        panel.Controls.Add(_keywordsTextBox, 1, 0);
        return panel;
    }

    private Control CreateOptionsRow()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
            WrapContents = false,
        };

        panel.Controls.Add(new Label { Text = "抓图模式", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        panel.Controls.Add(_captureModeComboBox);
        panel.Controls.Add(_normalizeLayoutCheckBox);
        panel.Controls.Add(_autoStartCcAppCheckBox);
        panel.Controls.Add(_performClickCheckBox);
        panel.Controls.Add(new Label { Text = "启动超时(秒)", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        panel.Controls.Add(_startupWaitNumeric);
        panel.Controls.Add(new Label { Text = "启动后等待(秒)", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        panel.Controls.Add(_postStartDelayNumeric);
        panel.Controls.Add(new Label { Text = "Enter后等待(秒)", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        panel.Controls.Add(_postEnterDelayNumeric);
        panel.Controls.Add(new Label { Text = "点击延迟(秒)", AutoSize = true, Padding = new Padding(0, 6, 0, 0) });
        panel.Controls.Add(_clickDelayNumeric);
        return panel;
    }

    private Control CreateActionRow()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
        };

        panel.Controls.Add(_runButton);
        panel.Controls.Add(_openMarkdownButton);
        panel.Controls.Add(_openJsonButton);
        panel.Controls.Add(_openImageButton);
        return panel;
    }

    private void InitializeDefaults()
    {
        var defaults = OcrProbeOptions.Parse(_launchArgs);
        _processPathTextBox.Text = defaults.ProcessPath;
        _keywordsTextBox.Text = string.Join(",", defaults.Keywords);
        _captureModeComboBox.Items.Clear();
        foreach (var option in _captureModeOptions)
        {
            _captureModeComboBox.Items.Add(option);
        }

        _captureModeComboBox.SelectedItem = _captureModeOptions.FirstOrDefault(option => option.Mode == defaults.CaptureMode) ?? _captureModeOptions[0];
        _normalizeLayoutCheckBox.Checked = defaults.NormalizeWindowLayout;
        _performClickCheckBox.Checked = defaults.PerformClick;
        _autoStartCcAppCheckBox.Checked = ParseBoolFromArgs(_launchArgs, "--auto-start-ccapp", true);
        _startupWaitNumeric.Value = Math.Clamp(ParseDurationSecondsFromArgs(_launchArgs, "--startup-wait-seconds", "--startup-wait-ms", 12), (int)_startupWaitNumeric.Minimum, (int)_startupWaitNumeric.Maximum);
        _postStartDelayNumeric.Value = Math.Clamp(ParseDurationSecondsFromArgs(_launchArgs, "--post-start-delay-seconds", "--post-start-delay-ms", 5), (int)_postStartDelayNumeric.Minimum, (int)_postStartDelayNumeric.Maximum);
        _postEnterDelayNumeric.Value = Math.Clamp(ParseDurationSecondsFromArgs(_launchArgs, "--post-enter-delay-seconds", "--post-enter-delay-ms", 3), (int)_postEnterDelayNumeric.Minimum, (int)_postEnterDelayNumeric.Maximum);
        _clickDelayNumeric.Value = Math.Clamp((int)Math.Round(defaults.ClickDelayMs / 1000.0), (int)_clickDelayNumeric.Minimum, (int)_clickDelayNumeric.Maximum);
        _resultTextBox.Text = "已就绪。请配置参数后点击“执行”。";
    }

    private void WireEvents()
    {
        _browseProcessButton.Click += (_, _) => BrowseProcessPath();
        _runButton.Click += async (_, _) => await RunProbeAsync();
        _openMarkdownButton.Click += (_, _) => OpenArtifact(_lastReport?.OutputMarkdownPath);
        _openJsonButton.Click += (_, _) => OpenArtifact(_lastReport?.OutputJsonPath);
        _openImageButton.Click += (_, _) => OpenArtifact(_lastReport?.CaptureImagePath);
        FormClosing += (_, _) => TrySaveUiState();
    }

    private async Task RunProbeAsync()
    {
        ToggleRunning(true);
        _statusLabel.Text = "执行中...";
        _resultTextBox.Clear();
        var runLog = new StringBuilder();

        void Log(string message)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            runLog.AppendLine(line);
            _resultTextBox.Text = runLog.ToString();
            _resultTextBox.SelectionStart = _resultTextBox.TextLength;
            _resultTextBox.ScrollToCaret();
        }

        try
        {
            var options = BuildOptions();
            TrySaveUiState(Log);
            Log($"CCApp 目标路径: {options.ProcessPath}");
            var startupResult = await EnsureCcAppReadyAsync(options, _autoStartCcAppCheckBox.Checked, ToMilliseconds(_startupWaitNumeric.Value), Log);
            if (startupResult.StartedByCurrentRun)
            {
                var postStartDelaySeconds = (int)_postStartDelayNumeric.Value;
                var postStartDelayMs = ToMilliseconds(_postStartDelayNumeric.Value);
                if (postStartDelayMs > 0)
                {
                    Log($"CCApp 已启动，等待登录界面稳定: {postStartDelaySeconds} 秒。");
                    await Task.Delay(postStartDelayMs);
                    Log("启动后等待完成。");
                }

                var enterResult = SendEnterToCcApp(options, Log);
                var postEnterDelaySeconds = (int)_postEnterDelayNumeric.Value;
                var postEnterDelayMs = ToMilliseconds(_postEnterDelayNumeric.Value);
                if (postEnterDelayMs > 0)
                {
                    Log($"Enter 已发送，等待 CCApp 响应: {postEnterDelaySeconds} 秒。");
                    await Task.Delay(postEnterDelayMs);
                    Log("Enter 后等待完成。");
                }

                ValidateEnterInput(enterResult, Log);
            }

            Log("CCApp 已就绪，开始执行 OCR。");
            var report = await OcrProbeExecution.RunAndPersistAsync(options);
            _lastReport = report;

            _statusLabel.Text = $"完成 {DateTime.Now:HH:mm:ss}";
            _openMarkdownButton.Enabled = true;
            _openJsonButton.Enabled = true;
            _openImageButton.Enabled = true;
            runLog.AppendLine();
            runLog.AppendLine(BuildResultSummary(report));
            _resultTextBox.Text = runLog.ToString();
        }
        catch (Exception exception)
        {
            _statusLabel.Text = "失败";
            runLog.AppendLine();
            runLog.AppendLine(exception.ToString());
            _resultTextBox.Text = runLog.ToString();
            MessageBox.Show(this, exception.Message, "OcrProbe", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            ToggleRunning(false);
        }
    }

    private void TryLoadUiState()
    {
        try
        {
            if (!File.Exists(_uiStateFilePath))
            {
                return;
            }

            var fileText = File.ReadAllText(_uiStateFilePath, Encoding.UTF8);
            var state = JsonSerializer.Deserialize<OcrProbeUiState>(fileText);
            if (state is null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(state.ProcessPath))
            {
                _processPathTextBox.Text = state.ProcessPath;
            }

            _keywordsTextBox.Text = state.Keywords ?? string.Empty;

            if (Enum.TryParse<CaptureMode>(state.CaptureMode, true, out var captureMode))
            {
                _captureModeComboBox.SelectedItem = _captureModeOptions.FirstOrDefault(option => option.Mode == captureMode) ?? _captureModeOptions[0];
            }

            _normalizeLayoutCheckBox.Checked = state.NormalizeWindowLayout;
            _autoStartCcAppCheckBox.Checked = state.AutoStartCcApp;
            _performClickCheckBox.Checked = state.PerformClick;
            _startupWaitNumeric.Value = ClampNumericValue(_startupWaitNumeric, state.StartupWaitSeconds);
            _postStartDelayNumeric.Value = ClampNumericValue(_postStartDelayNumeric, state.PostStartDelaySeconds);
            _postEnterDelayNumeric.Value = ClampNumericValue(_postEnterDelayNumeric, state.PostEnterDelaySeconds);
            _clickDelayNumeric.Value = ClampNumericValue(_clickDelayNumeric, state.ClickDelaySeconds);
            _resultTextBox.Text = "已加载上次参数。";
        }
        catch
        {
            _resultTextBox.Text = "读取上次参数失败，已使用默认参数。";
        }
    }

    private void TrySaveUiState(Action<string>? log = null)
    {
        try
        {
            var selectedCaptureMode = (_captureModeComboBox.SelectedItem as CaptureModeOption)?.Mode ?? CaptureMode.Window;
            var state = new OcrProbeUiState
            {
                ProcessPath = _processPathTextBox.Text.Trim(),
                Keywords = _keywordsTextBox.Text.Trim(),
                CaptureMode = selectedCaptureMode.ToString(),
                NormalizeWindowLayout = _normalizeLayoutCheckBox.Checked,
                AutoStartCcApp = _autoStartCcAppCheckBox.Checked,
                PerformClick = _performClickCheckBox.Checked,
                StartupWaitSeconds = (int)_startupWaitNumeric.Value,
                PostStartDelaySeconds = (int)_postStartDelayNumeric.Value,
                PostEnterDelaySeconds = (int)_postEnterDelayNumeric.Value,
                ClickDelaySeconds = (int)_clickDelayNumeric.Value,
            };

            var parentDirectory = Path.GetDirectoryName(_uiStateFilePath);
            if (!string.IsNullOrWhiteSpace(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            var json = JsonSerializer.Serialize(
                state,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });

            File.WriteAllText(_uiStateFilePath, json, new UTF8Encoding(false));
            log?.Invoke($"已记忆当前参数: {_uiStateFilePath}");
        }
        catch (Exception exception)
        {
            log?.Invoke($"警告: 参数记忆失败: {exception.Message}");
        }
    }

    private static decimal ClampNumericValue(NumericUpDown control, int value)
    {
        var numericValue = value;
        if (numericValue < control.Minimum)
        {
            return control.Minimum;
        }

        if (numericValue > control.Maximum)
        {
            return control.Maximum;
        }

        return numericValue;
    }

    private OcrProbeOptions BuildOptions()
    {
        if (string.IsNullOrWhiteSpace(_processPathTextBox.Text))
        {
            throw new InvalidOperationException("进程路径不能为空。");
        }

        var args = new List<string>(_launchArgs);
        var selectedCaptureMode = (_captureModeComboBox.SelectedItem as CaptureModeOption)?.Mode ?? CaptureMode.Window;
        AppendOption(args, "--process-path", _processPathTextBox.Text.Trim());
        AppendOption(args, "--keywords", _keywordsTextBox.Text.Trim());
        AppendOption(args, "--capture-mode", selectedCaptureMode.ToString());
        AppendOption(args, "--normalize-window-layout", _normalizeLayoutCheckBox.Checked ? "true" : "false");
        AppendOption(args, "--perform-click", _performClickCheckBox.Checked ? "true" : "false");
        AppendOption(args, "--click-delay-ms", ToMilliseconds(_clickDelayNumeric.Value).ToString(CultureInfo.InvariantCulture));
        return OcrProbeOptions.Parse(args.ToArray());
    }

    private static int ToMilliseconds(decimal seconds)
    {
        return (int)Math.Round((double)seconds * 1000d);
    }

    private static void AppendOption(List<string> args, string key, string value)
    {
        args.Add(key);
        args.Add(value);
    }

    private static async Task<CcAppStartupResult> EnsureCcAppReadyAsync(OcrProbeOptions options, bool autoStartWhenMissing, int startupWaitMs, Action<string> log)
    {
        var existingProcessId = FindRunningCcAppProcessId(options);
        if (existingProcessId.HasValue)
        {
            log($"检测到 CCApp 已运行，PID={existingProcessId.Value}。");
            return new CcAppStartupResult(existingProcessId.Value, StartedByCurrentRun: false);
        }

        if (!autoStartWhenMissing)
        {
            throw new InvalidOperationException($"未找到匹配进程: {options.ProcessPath}");
        }

        if (!File.Exists(options.ProcessPath))
        {
            throw new InvalidOperationException($"未找到 CCApp 可执行文件: {options.ProcessPath}。请先在 UI 中通过 Browse 选择正确路径。");
        }

        log("未检测到运行中的 CCApp，正在尝试启动。");
        var startInfo = new ProcessStartInfo(options.ProcessPath)
        {
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(options.ProcessPath) ?? Environment.CurrentDirectory,
        };

        using var startedProcess = Process.Start(startInfo);
        if (startedProcess is null)
        {
            throw new InvalidOperationException("CCApp 启动失败：Process.Start 返回空。请确认路径和权限。");
        }

        log($"已发起 CCApp 启动请求，PID={startedProcess.Id}，等待最多 {startupWaitMs / 1000.0:0.#} 秒。");
        var elapsed = 0;
        const int pollIntervalMs = 500;
        while (elapsed < startupWaitMs)
        {
            await Task.Delay(pollIntervalMs);
            elapsed += pollIntervalMs;

            var processId = FindRunningCcAppProcessId(options);
            if (processId.HasValue)
            {
                log($"检测到 CCApp 启动完成，PID={processId.Value}，耗时约 {elapsed} ms。");
                return new CcAppStartupResult(processId.Value, StartedByCurrentRun: true);
            }

            log($"等待 CCApp 启动中... {elapsed / 1000.0:0.#}/{startupWaitMs / 1000.0:0.#} 秒");
        }

        throw new InvalidOperationException($"CCApp 启动超时：等待 {startupWaitMs / 1000.0:0.#} 秒后仍未检测到目标进程。路径: {options.ProcessPath}");
    }

    private readonly record struct CcAppStartupResult(int ProcessId, bool StartedByCurrentRun);

    private static EnterInputResult SendEnterToCcApp(OcrProbeOptions options, Action<string> log)
    {
        if (TryBringCcAppToForeground(options, out var focusMessage, out var processId, out var windowHandle))
        {
            log(focusMessage);
        }
        else
        {
            log($"警告: {focusMessage}；将向当前活动窗口发送 Enter。");
        }

        if (!TrySendEnterInput(out var sendMessage))
        {
            throw new InvalidOperationException($"发送 Enter 按键失败: {sendMessage}");
        }

        log($"已发送 Enter 按键。{sendMessage}");
        return new EnterInputResult(processId, windowHandle == IntPtr.Zero ? null : windowHandle.ToInt64(), true);
    }

    private static void ValidateEnterInput(EnterInputResult result, Action<string> log)
    {
        if (!result.InjectionSucceeded)
        {
            log("警告: Enter 输入注入未成功。请检查。");
            return;
        }

        if (result.TargetProcessId.HasValue && !IsProcessRunning(result.TargetProcessId.Value))
        {
            log($"警告: Enter 发送后 CCApp 进程已退出，PID={result.TargetProcessId.Value}。");
            return;
        }

        var foregroundWindow = GetForegroundWindow();
        if (foregroundWindow == IntPtr.Zero)
        {
            log("Enter 输入检查结果: 输入注入成功，但无法读取当前前台窗口。");
            return;
        }

        _ = GetWindowThreadProcessId(foregroundWindow, out var foregroundPid);
        if (result.TargetProcessId.HasValue && foregroundPid == (uint)result.TargetProcessId.Value)
        {
            log($"Enter 输入检查通过: 前台窗口仍为 CCApp，PID={foregroundPid}。");
            return;
        }

        if (result.TargetProcessId.HasValue)
        {
            log($"Enter 输入检查提示: 输入注入成功，但当前前台 PID={foregroundPid}，目标 CCApp PID={result.TargetProcessId.Value}。");
            return;
        }

        log($"Enter 输入检查结果: 输入注入成功，当前前台 PID={foregroundPid}。");
    }

    private static bool TryBringCcAppToForeground(OcrProbeOptions options, out string message, out int? processId, out IntPtr windowHandle)
    {
        processId = FindRunningCcAppProcessId(options);
        windowHandle = IntPtr.Zero;
        if (!processId.HasValue)
        {
            message = "未找到运行中的 CCApp 进程";
            return false;
        }

        try
        {
            using var process = Process.GetProcessById(processId.Value);
            for (var attempt = 0; attempt < 20; attempt++)
            {
                process.Refresh();
                windowHandle = process.MainWindowHandle;
                if (windowHandle != IntPtr.Zero)
                {
                    break;
                }

                Thread.Sleep(200);
            }

            if (windowHandle == IntPtr.Zero)
            {
                message = $"CCApp PID={processId.Value} 尚未暴露主窗口句柄";
                return false;
            }

            ShowWindow(windowHandle, SwRestore);
            var activated = SetForegroundWindow(windowHandle);
            if (!activated)
            {
                message = $"CCApp 主窗口激活失败，HWND=0x{windowHandle.ToInt64():X}";
                return false;
            }

            message = $"CCApp 主窗口已激活，HWND=0x{windowHandle.ToInt64():X}";
            return true;
        }
        catch (Exception exception)
        {
            message = $"激活 CCApp 失败: {exception.Message}";
            windowHandle = IntPtr.Zero;
            return false;
        }
    }

    private static bool TrySendEnterInput(out string message)
    {
        var inputs = new[]
        {
            new INPUT
            {
                type = InputTypeKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VkReturn,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero,
                    },
                },
            },
            new INPUT
            {
                type = InputTypeKeyboard,
                U = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = VkReturn,
                        wScan = 0,
                        dwFlags = KeyEventFKeyUp,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero,
                    },
                },
            },
        };

        var sentCount = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        if (sentCount == inputs.Length)
        {
            message = "SendInput 成功。";
            return true;
        }

        message = $"SendInput 返回 {sentCount}/{inputs.Length}。";
        return false;
    }

    private static bool IsProcessRunning(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return !process.HasExited;
        }
        catch
        {
            return false;
        }
    }

    private static int? FindRunningCcAppProcessId(OcrProbeOptions options)
    {
        var normalizedPath = Path.GetFullPath(options.ProcessPath);
        foreach (var process in Process.GetProcessesByName(options.ProcessName))
        {
            using (process)
            {
                var executablePath = TryGetExecutablePath(process);
                if (string.IsNullOrWhiteSpace(executablePath))
                {
                    continue;
                }

                if (string.Equals(Path.GetFullPath(executablePath), normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    return process.Id;
                }
            }
        }

        return null;
    }

    private static string? TryGetExecutablePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static bool ParseBoolFromArgs(IReadOnlyList<string> args, string key, bool fallback)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (!string.Equals(args[index], key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (bool.TryParse(args[index + 1], out var parsedBool))
            {
                return parsedBool;
            }

            if (int.TryParse(args[index + 1], out var parsedInt))
            {
                return parsedInt != 0;
            }
        }

        return fallback;
    }

    private static int ParseDurationSecondsFromArgs(IReadOnlyList<string> args, string secondsKey, string millisecondsKey, int fallbackSeconds)
    {
        var seconds = ParseIntFromArgs(args, secondsKey);
        if (seconds.HasValue)
        {
            return Math.Max(0, seconds.Value);
        }

        var milliseconds = ParseIntFromArgs(args, millisecondsKey);
        if (milliseconds.HasValue)
        {
            return Math.Max(0, (int)Math.Round(milliseconds.Value / 1000.0));
        }

        return fallbackSeconds;
    }

    private static int? ParseIntFromArgs(IReadOnlyList<string> args, string key)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (!string.Equals(args[index], key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(args[index + 1], out var parsedValue))
            {
                return parsedValue;
            }
        }

        return null;
    }

    private void BrowseProcessPath()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = _processPathTextBox.Text,
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _processPathTextBox.Text = dialog.FileName;
        }
    }

    private static string BuildResultSummary(OcrReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Markdown 报告: {report.OutputMarkdownPath}");
        builder.AppendLine($"JSON 结果: {report.OutputJsonPath}");
        builder.AppendLine($"截图文件: {report.CaptureImagePath}");
        builder.AppendLine($"PID: {report.Process.ProcessId}");
        builder.AppendLine($"窗口标题: {report.Process.MainWindowTitle}");
        builder.AppendLine($"识别状态: {report.JsonSummary.StateDetection.DetectedState ?? "<none>"}");
        builder.AppendLine($"命中关键词: {(report.JsonSummary.CustomKeywordDetection.MatchedKeywords.Count == 0 ? "<none>" : string.Join(", ", report.JsonSummary.CustomKeywordDetection.MatchedKeywords))}");
        builder.AppendLine($"已请求点击: {report.JsonSummary.ClickAction.Requested}");
        builder.AppendLine($"已执行点击: {report.JsonSummary.ClickAction.Executed}");
        builder.AppendLine($"点击结果: {report.JsonSummary.ClickAction.Message}");
        if (report.JsonSummary.ClickAction.Executed)
        {
            builder.AppendLine($"点击坐标: ({report.JsonSummary.ClickAction.ScreenX}, {report.JsonSummary.ClickAction.ScreenY})");
        }

        builder.AppendLine();
        builder.AppendLine("OCR 文本:");
        builder.AppendLine(report.JsonSummary.Text.Original);
        return builder.ToString();
    }

    private static void OpenArtifact(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            MessageBox.Show("未找到输出文件。", "OcrProbe", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private void ToggleRunning(bool running)
    {
        _runButton.Enabled = !running;
        _browseProcessButton.Enabled = !running;
        _processPathTextBox.Enabled = !running;
        _keywordsTextBox.Enabled = !running;
        _captureModeComboBox.Enabled = !running;
        _normalizeLayoutCheckBox.Enabled = !running;
        _autoStartCcAppCheckBox.Enabled = !running;
        _performClickCheckBox.Enabled = !running;
        _startupWaitNumeric.Enabled = !running;
        _postStartDelayNumeric.Enabled = !running;
        _postEnterDelayNumeric.Enabled = !running;
        _clickDelayNumeric.Enabled = !running;
    }

    private sealed record CaptureModeOption(CaptureMode Mode, string Display)
    {
        public override string ToString() => Display;
    }

    private sealed class OcrProbeUiState
    {
        public string? ProcessPath { get; set; }

        public string? Keywords { get; set; }

        public string? CaptureMode { get; set; }

        public bool NormalizeWindowLayout { get; set; }

        public bool AutoStartCcApp { get; set; }

        public bool PerformClick { get; set; }

        public int StartupWaitSeconds { get; set; }

        public int PostStartDelaySeconds { get; set; }

        public int PostEnterDelaySeconds { get; set; }

        public int ClickDelaySeconds { get; set; }
    }

    private readonly record struct EnterInputResult(int? TargetProcessId, long? TargetWindowHandle, bool InjectionSucceeded);

    private const int SwRestore = 9;
    private const uint InputTypeKeyboard = 1;
    private const ushort VkReturn = 0x0D;
    private const uint KeyEventFKeyUp = 0x0002;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint numberOfInputs, INPUT[] inputs, int sizeOfInputStructure);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
