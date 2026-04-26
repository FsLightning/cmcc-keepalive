using System.Diagnostics;
using GuardService.Configuration;
using GuardService.Models;
using Microsoft.Extensions.Options;

namespace GuardService.Automation;

public sealed class ProcessController
{
    private readonly GuardOptions _options;
    private readonly ILogger<ProcessController> _logger;

    public ProcessController(IOptions<GuardOptions> options, ILogger<ProcessController> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public CycleAction TryStartTargetProcess()
    {
        var observedAt = DateTimeOffset.Now;
        if (string.IsNullOrWhiteSpace(_options.TargetExecutablePath))
        {
            return new CycleAction(
                observedAt,
                CycleActionType.StartProcess,
                false,
                "未配置 TargetExecutablePath，无法自动启动目标进程。",
                "ProcessStart");
        }

        var path = _options.TargetExecutablePath;
        if (!File.Exists(path))
        {
            return new CycleAction(
                observedAt,
                CycleActionType.StartProcess,
                false,
                "目标可执行文件不存在。",
                "ProcessStart",
                path);
        }

        try
        {
            var startInfo = new ProcessStartInfo(path)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(path) ?? Environment.CurrentDirectory,
            };

            using var process = Process.Start(startInfo);
            if (process is null)
            {
                return new CycleAction(
                    observedAt,
                    CycleActionType.StartProcess,
                    false,
                    "进程启动失败，Process.Start 返回空。",
                    "ProcessStart",
                    path);
            }

            _logger.LogInformation("Auto-started target process. pid={ProcessId}, path={Path}", process.Id, path);
            return new CycleAction(
                observedAt,
                CycleActionType.StartProcess,
                true,
                "目标进程已启动。",
                "ProcessStart",
                $"pid={process.Id}");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to auto-start target process. path={Path}", path);
            return new CycleAction(
                observedAt,
                CycleActionType.StartProcess,
                false,
                "自动启动目标进程失败。",
                "ProcessStart",
                exception.Message);
        }
    }

    public CycleAction TryKillSelectedProcess(ProcessSnapshot processSnapshot, string reason)
    {
        var observedAt = DateTimeOffset.Now;
        if (!processSnapshot.IsRunning || !processSnapshot.ProcessId.HasValue)
        {
            return new CycleAction(
                observedAt,
                CycleActionType.KillProcess,
                false,
                "没有可关闭的目标进程。",
                "ProcessKill",
                reason);
        }

        try
        {
            using var process = Process.GetProcessById(processSnapshot.ProcessId.Value);
            process.Kill(true);
            process.WaitForExit(5000);
            _logger.LogInformation("Killed target process for test mode. pid={ProcessId}, reason={Reason}", process.Id, reason);

            return new CycleAction(
                observedAt,
                CycleActionType.KillProcess,
                true,
                "目标进程已关闭。",
                "ProcessKill",
                $"pid={processSnapshot.ProcessId.Value}; reason={reason}");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Failed to kill target process. pid={ProcessId}, reason={Reason}",
                processSnapshot.ProcessId.Value,
                reason);

            return new CycleAction(
                observedAt,
                CycleActionType.KillProcess,
                false,
                "关闭目标进程失败。",
                "ProcessKill",
                exception.Message);
        }
    }
}
