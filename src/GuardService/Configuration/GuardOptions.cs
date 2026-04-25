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
