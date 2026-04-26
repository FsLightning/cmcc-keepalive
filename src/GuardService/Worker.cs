using System.Text.Json;
using GuardService.Automation;
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
    private readonly ProcessController _processController;
    private readonly LoginAssist _loginAssist;
    private readonly GuardOptions _options;
    private int _testLoopsCompleted;
    private bool _testModeCompletionLogged;

    public Worker(
        ILogger<Worker> logger,
        ProcessMonitor processMonitor,
        WindowProbe windowProbe,
        SessionClassifier sessionClassifier,
        ProcessController processController,
        LoginAssist loginAssist,
        IOptions<GuardOptions> options)
    {
        _logger = logger;
        _processMonitor = processMonitor;
        _windowProbe = windowProbe;
        _sessionClassifier = sessionClassifier;
        _processController = processController;
        _loginAssist = loginAssist;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Recognition worker started. PollIntervalSeconds={PollIntervalSeconds}, TargetProcessName={TargetProcessName}, TargetExecutablePath={TargetExecutablePath}, AutoStart={AutoStart}, LoginAssist={EnableLoginAssist}, TestMode={EnableTestMode}, TestModeLoops={TestModeLoopCount}, HeadlessWindowOnly={HeadlessWindowOnly}",
            _options.PollIntervalSeconds,
            _options.TargetProcessName,
            _options.TargetExecutablePath ?? "<not configured>",
            _options.AutoStartWhenNotRunning,
            _options.EnableLoginAssist,
            _options.EnableTestMode,
            _options.TestModeLoopCount,
            _options.HeadlessWindowOnly);

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
            var actions = new List<CycleAction>();
            var processSnapshot = await _processMonitor.ProbeAsync(stoppingToken);

            if (!processSnapshot.IsRunning && _options.AutoStartWhenNotRunning)
            {
                var startAction = _processController.TryStartTargetProcess();
                actions.Add(startAction);

                if (startAction.Succeeded)
                {
                    await Task.Delay(_options.StartProcessWaitMilliseconds, stoppingToken);
                    processSnapshot = await _processMonitor.ProbeAsync(stoppingToken);
                }
            }

            var windowSnapshot = await _windowProbe.ProbeAsync(processSnapshot, stoppingToken);
            var sessionState = _sessionClassifier.Classify(processSnapshot, windowSnapshot);

            if (ShouldAttemptLoginAssist(processSnapshot, windowSnapshot, sessionState))
            {
                var loginAction = _loginAssist.TryClickLogin(processSnapshot, windowSnapshot);
                actions.Add(loginAction);

                if (loginAction.Succeeded)
                {
                    await Task.Delay(_options.LoginClickTimeoutMs, stoppingToken);
                    processSnapshot = await _processMonitor.ProbeAsync(stoppingToken);
                    windowSnapshot = await _windowProbe.ProbeAsync(processSnapshot, stoppingToken);
                    sessionState = _sessionClassifier.Classify(processSnapshot, windowSnapshot);
                }
            }

            if (ShouldRunTestModeKill(processSnapshot, sessionState))
            {
                var killAction = _processController.TryKillSelectedProcess(processSnapshot, "DesktopReady reached in test mode.");
                actions.Add(killAction);

                if (killAction.Succeeded)
                {
                    _testLoopsCompleted++;
                    processSnapshot = ProcessSnapshot.CreateNotRunning(DateTimeOffset.Now, _options.TargetProcessName, processSnapshot.Candidates);
                    windowSnapshot = WindowSnapshot.CreateMissing(DateTimeOffset.Now, "Process was terminated by test mode.");
                    sessionState = SessionState.NotRunning;
                }
            }

            if (_options.EnableTestMode &&
                !_testModeCompletionLogged &&
                _testLoopsCompleted >= _options.TestModeLoopCount)
            {
                _testModeCompletionLogged = true;
                _logger.LogInformation(
                    "Test mode loop target reached. completed={CompletedLoops}, target={TargetLoops}",
                    _testLoopsCompleted,
                    _options.TestModeLoopCount);
            }

            var result = new GuardCycleResult(
                DateTimeOffset.Now,
                processSnapshot,
                windowSnapshot,
                sessionState,
                actions,
                _options.EnableTestMode ? _testLoopsCompleted : null,
                _options.EnableTestMode ? _options.TestModeLoopCount : null);

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

    private bool ShouldAttemptLoginAssist(ProcessSnapshot processSnapshot, WindowSnapshot windowSnapshot, SessionState sessionState)
    {
        if (!_options.EnableLoginAssist)
        {
            return false;
        }

        if (!processSnapshot.IsRunning || !windowSnapshot.HasWindow)
        {
            return false;
        }

        if (sessionState == SessionState.DesktopReady)
        {
            return false;
        }

        if (_options.EnableTestMode && _testLoopsCompleted >= _options.TestModeLoopCount)
        {
            return false;
        }

        return true;
    }

    private bool ShouldRunTestModeKill(ProcessSnapshot processSnapshot, SessionState sessionState)
    {
        return _options.EnableTestMode &&
            _testLoopsCompleted < _options.TestModeLoopCount &&
            processSnapshot.IsRunning &&
            sessionState == SessionState.DesktopReady;
    }
}
