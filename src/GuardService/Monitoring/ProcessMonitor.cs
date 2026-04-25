using System.Diagnostics;
using GuardService.Configuration;
using GuardService.Models;
using Microsoft.Extensions.Options;

namespace GuardService.Monitoring;

public sealed class ProcessMonitor
{
    private readonly GuardOptions _options;
    private readonly ILogger<ProcessMonitor> _logger;

    public ProcessMonitor(IOptions<GuardOptions> options, ILogger<ProcessMonitor> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<ProcessSnapshot> ProbeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var observedAt = DateTimeOffset.Now;
        var normalizedName = GuardOptions.NormalizeProcessName(_options.TargetProcessName);
        var processes = Process.GetProcessesByName(normalizedName);
        var candidates = new List<ProcessCandidate>(processes.Length);

        foreach (var process in processes)
        {
            candidates.Add(BuildCandidate(process));
        }

        if (candidates.Count == 0)
        {
            return Task.FromResult(ProcessSnapshot.CreateNotRunning(observedAt, _options.TargetProcessName));
        }

        var selectedCandidate = SelectCandidate(candidates);
        if (selectedCandidate is null)
        {
            _logger.LogWarning(
                "Found {CandidateCount} process candidates for {ProcessName}, but none matched the configured executable path.",
                candidates.Count,
                _options.TargetProcessName);

            return Task.FromResult(ProcessSnapshot.CreateNotRunning(observedAt, _options.TargetProcessName, candidates));
        }

        var snapshot = new ProcessSnapshot(
            observedAt,
            _options.TargetProcessName,
            ProcessDetectionState.Running,
            selectedCandidate.ProcessId,
            selectedCandidate.ExecutablePath,
            selectedCandidate.StartTime,
            selectedCandidate.HasMainWindow,
            selectedCandidate.SelectionReason,
            candidates);

        return Task.FromResult(snapshot);
    }

    private ProcessCandidate? SelectCandidate(IReadOnlyList<ProcessCandidate> candidates)
    {
        if (!string.IsNullOrWhiteSpace(_options.TargetExecutablePath))
        {
            var exactPathMatch = candidates.FirstOrDefault(candidate =>
                string.Equals(candidate.ExecutablePath, _options.TargetExecutablePath, StringComparison.OrdinalIgnoreCase));

            if (exactPathMatch is not null)
            {
                return exactPathMatch with { SelectionReason = "Selected by exact executable path match." };
            }

            return null;
        }

        if (candidates.Count == 1)
        {
            return candidates[0] with { SelectionReason = "Selected because it is the only matching process." };
        }

        var mainWindowCandidate = candidates.Where(candidate => candidate.HasMainWindow).ToList();
        if (mainWindowCandidate.Count == 1)
        {
            return mainWindowCandidate[0] with { SelectionReason = "Selected because it is the only candidate with a main window handle." };
        }

        var latestCandidate = candidates
            .OrderByDescending(candidate => candidate.StartTime ?? DateTimeOffset.MinValue)
            .First();

        return latestCandidate with { SelectionReason = "Selected as the most recently started matching process candidate." };
    }

    private static ProcessCandidate BuildCandidate(Process process)
    {
        var executablePath = TryGetValue(() => process.MainModule?.FileName);
        var startTime = TryGetValue(() => new DateTimeOffset(process.StartTime));
        var hasMainWindow = process.MainWindowHandle != IntPtr.Zero;

        return new ProcessCandidate(
            process.Id,
            executablePath,
            startTime,
            hasMainWindow,
            "Observed process candidate.");
    }

    private static T? TryGetValue<T>(Func<T?> accessor)
    {
        try
        {
            return accessor();
        }
        catch (InvalidOperationException)
        {
            return default;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return default;
        }
        catch (NotSupportedException)
        {
            return default;
        }
    }
}
