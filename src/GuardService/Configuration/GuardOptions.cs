using System.ComponentModel.DataAnnotations;
using System.IO;

namespace GuardService.Configuration;

public sealed class GuardOptions
{
    [Range(1, 3600)]
    public int PollIntervalSeconds { get; set; } = 30;

    [Required]
    public string TargetProcessName { get; set; } = "Ecloud Cloud Computer Application.exe";

    public string? TargetExecutablePath { get; set; }

    public List<string> DesktopTitleKeywords { get; set; } = [];

    public List<string> DesktopClassKeywords { get; set; } = [];

    [Range(1, 20000)]
    public int MinimumDesktopWidth { get; set; } = 1000;

    [Range(1, 20000)]
    public int MinimumDesktopHeight { get; set; } = 700;

    public bool RequireVisibleWindow { get; set; } = true;

    public bool AllowMinimizedWindow { get; set; }

    public bool AutoStartWhenNotRunning { get; set; } = true;

    [Range(100, 300000)]
    public int StartProcessWaitMilliseconds { get; set; } = 5000;

    public bool EnableLoginAssist { get; set; } = true;

    [Range(100, 300000)]
    public int LoginClickTimeoutMs { get; set; } = 3000;

    [Range(1, 3600)]
    public int LoginAssistCooldownSeconds { get; set; } = 10;

    public bool AllowMouseFallback { get; set; } = true;

    public List<string> LoginButtonKeywords { get; set; } =
    [
        "登录",
        "立即登录",
        "Login",
    ];

    public bool EnableTestMode { get; set; }

    [Range(1, 1000)]
    public int TestModeLoopCount { get; set; } = 3;

    public bool HeadlessWindowOnly { get; set; } = true;

    public string? AutomationDiagnosticsPath { get; set; }

    public string NormalizedProcessName => NormalizeProcessName(TargetProcessName);

    public static string NormalizeProcessName(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return string.Empty;
        }

        var fileName = Path.GetFileName(processName.Trim());
        return Path.GetFileNameWithoutExtension(fileName);
    }
}
