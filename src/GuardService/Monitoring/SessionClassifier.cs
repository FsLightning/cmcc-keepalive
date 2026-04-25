using GuardService.Configuration;
using GuardService.Models;
using Microsoft.Extensions.Options;

namespace GuardService.Monitoring;

public sealed class SessionClassifier
{
    private readonly GuardOptions _options;

    public SessionClassifier(IOptions<GuardOptions> options)
    {
        _options = options.Value;
    }

    public SessionState Classify(ProcessSnapshot processSnapshot, WindowSnapshot windowSnapshot)
    {
        if (!processSnapshot.IsRunning)
        {
            return SessionState.NotRunning;
        }

        if (!windowSnapshot.HasWindow || windowSnapshot.Bounds is null)
        {
            return SessionState.ProcessOnly;
        }

        if (IsDesktopReady(windowSnapshot))
        {
            return SessionState.DesktopReady;
        }

        return SessionState.ClientVisibleButUnknown;
    }

    private bool IsDesktopReady(WindowSnapshot windowSnapshot)
    {
        if (_options.RequireVisibleWindow && windowSnapshot.IsVisible != true)
        {
            return false;
        }

        if (!_options.AllowMinimizedWindow && windowSnapshot.IsMinimized == true)
        {
            return false;
        }

        var bounds = windowSnapshot.Bounds;
        if (bounds is null || bounds.Width < _options.MinimumDesktopWidth || bounds.Height < _options.MinimumDesktopHeight)
        {
            return false;
        }

        var titleMatches = MatchesAnyKeyword(windowSnapshot.Title, _options.DesktopTitleKeywords);
        var classMatches = MatchesAnyKeyword(windowSnapshot.ClassName, _options.DesktopClassKeywords);

        return titleMatches || classMatches || NoKeywordsConfigured();
    }

    private bool NoKeywordsConfigured()
    {
        return _options.DesktopTitleKeywords.Count == 0 && _options.DesktopClassKeywords.Count == 0;
    }

    private static bool MatchesAnyKeyword(string? source, IReadOnlyCollection<string> keywords)
    {
        if (keywords.Count == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(source))
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
}
