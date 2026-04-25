using System.Text.Json;
using GuardService.Configuration;
using GuardService.Models;
using GuardService.Monitoring;
using Microsoft.Extensions.Options;

namespace GuardService;

public sealed class Worker : BackgroundService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ILogger<Worker> _logger;
    private readonly ProcessMonitor _processMonitor;
    private readonly WindowProbe _windowProbe;
    private readonly SessionClassifier _sessionClassifier;
    private readonly GuardOptions _options;

    public Worker(
        ILogger<Worker> logger,
        ProcessMonitor processMonitor,
        WindowProbe windowProbe,
        SessionClassifier sessionClassifier,
        IOptions<GuardOptions> options)
    {
        _logger = logger;
        _processMonitor = processMonitor;
        _windowProbe = windowProbe;
        _sessionClassifier = sessionClassifier;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Recognition MVP started. PollIntervalSeconds={PollIntervalSeconds}, TargetProcessName={TargetProcessName}, TargetExecutablePath={TargetExecutablePath}",
            _options.PollIntervalSeconds,
            _options.TargetProcessName,
            _options.TargetExecutablePath ?? "<not configured>");

        await RunCycleAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.PollIntervalSeconds));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleAsync(stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            var processSnapshot = await _processMonitor.ProbeAsync(stoppingToken);
            var windowSnapshot = await _windowProbe.ProbeAsync(processSnapshot, stoppingToken);
            var sessionState = _sessionClassifier.Classify(processSnapshot, windowSnapshot);

            var result = new GuardCycleResult(
                DateTimeOffset.Now,
                processSnapshot,
                windowSnapshot,
                sessionState);

            _logger.LogInformation(
                "Recognition cycle completed. {Summary} payload={Payload}",
                result.ToSummary(),
                JsonSerializer.Serialize(result, SerializerOptions));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Recognition cycle failed.");
        }
    }
}
