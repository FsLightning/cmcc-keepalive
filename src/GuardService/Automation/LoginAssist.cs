using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using GuardService.Configuration;
using GuardService.Models;
using Microsoft.Extensions.Options;

namespace GuardService.Automation;

public sealed class LoginAssist
{
    private readonly GuardOptions _options;
    private readonly ILogger<LoginAssist> _logger;
    private DateTimeOffset _lastAttemptAt = DateTimeOffset.MinValue;

    public LoginAssist(IOptions<GuardOptions> options, ILogger<LoginAssist> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public CycleAction TryClickLogin(ProcessSnapshot processSnapshot, WindowSnapshot windowSnapshot)
    {
        var observedAt = DateTimeOffset.Now;
        if (!OperatingSystem.IsWindows())
        {
            return new CycleAction(
                observedAt,
                CycleActionType.LoginClick,
                false,
                "当前系统不是 Windows，无法执行登录按钮动作。",
                "UIA");
        }

        if (!processSnapshot.IsRunning || !windowSnapshot.HasWindow || !windowSnapshot.Handle.HasValue)
        {
            return new CycleAction(
                observedAt,
                CycleActionType.LoginClick,
                false,
                "没有可用于登录辅助动作的主窗口。",
                "UIA");
        }

        if (!ShouldAttemptNow(observedAt))
        {
            return new CycleAction(
                observedAt,
                CycleActionType.Skip,
                true,
                "登录辅助动作处于冷却期，跳过本轮。",
                "Cooldown");
        }

        _lastAttemptAt = observedAt;

        try
        {
            using var automation = new UIA3Automation();
            var root = automation.FromHandle(new IntPtr(windowSnapshot.Handle.Value));
            if (root is null)
            {
                return new CycleAction(
                    observedAt,
                    CycleActionType.LoginClick,
                    false,
                    "无法从主窗口句柄获取 UIA 根节点。",
                    "UIA");
            }

            var matchedButton = FindMatchedButton(root, _options.LoginButtonKeywords);
            if (matchedButton is null)
            {
                return new CycleAction(
                    observedAt,
                    CycleActionType.LoginClick,
                    false,
                    "没有找到匹配登录关键词的按钮控件。",
                    "UIA");
            }

            if (matchedButton.Patterns.Invoke.IsSupported)
            {
                matchedButton.Patterns.Invoke.Pattern.Invoke();
                _logger.LogInformation("Clicked login button through UIA invoke. name={ButtonName}", matchedButton.Name);
                return new CycleAction(
                    observedAt,
                    CycleActionType.LoginClick,
                    true,
                    "已通过 UIA Invoke 触发登录按钮。",
                    "UIAInvoke",
                    matchedButton.Name);
            }

            if (_options.AllowMouseFallback && TryMouseClickElementCenter(matchedButton, out var clickDetails))
            {
                _logger.LogInformation("Clicked login button with mouse fallback. details={Details}", clickDetails);
                return new CycleAction(
                    observedAt,
                    CycleActionType.LoginClick,
                    true,
                    "UIA Invoke 不可用，已使用鼠标回退点击登录按钮。",
                    "MouseFallback",
                    clickDetails);
            }

            return new CycleAction(
                observedAt,
                CycleActionType.LoginClick,
                false,
                "登录按钮已匹配，但既不能 Invoke 也不能执行鼠标回退。",
                "UIA");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Login assist action failed.");
            return new CycleAction(
                observedAt,
                CycleActionType.LoginClick,
                false,
                "执行登录按钮动作失败。",
                "UIA",
                exception.Message);
        }
    }

    private bool ShouldAttemptNow(DateTimeOffset now)
    {
        return (now - _lastAttemptAt).TotalSeconds >= _options.LoginAssistCooldownSeconds;
    }

    private static AutomationElement? FindMatchedButton(AutomationElement root, IReadOnlyCollection<string> keywords)
    {
        var buttonElements = root.FindAllDescendants(conditionFactory => conditionFactory.ByControlType(ControlType.Button));
        foreach (var button in buttonElements)
        {
            if (ContainsAnyKeyword(button.Name, keywords))
            {
                return button;
            }
        }

        return null;
    }

    private static bool ContainsAnyKeyword(string? source, IReadOnlyCollection<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(source) || keywords.Count == 0)
        {
            return false;
        }

        foreach (var keyword in keywords)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                continue;
            }

            if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryMouseClickElementCenter(AutomationElement element, out string details)
    {
        details = string.Empty;
        var bounds = element.BoundingRectangle;
        if (bounds.IsEmpty)
        {
            return false;
        }

        var x = (int)Math.Round(bounds.X + bounds.Width / 2.0);
        var y = (int)Math.Round(bounds.Y + bounds.Height / 2.0);
        if (!SetCursorPos(x, y))
        {
            return false;
        }

        mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
        details = $"x={x},y={y},name={element.Name}";
        return true;
    }

    private const uint MouseEventLeftDown = 0x0002;
    private const uint MouseEventLeftUp = 0x0004;

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);
}
