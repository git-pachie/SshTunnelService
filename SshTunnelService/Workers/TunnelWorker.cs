using System.Collections.Concurrent;
using Microsoft.Extensions.Hosting;
using SshTunnelService.Models;
using SshTunnelService.Services.Interfaces;

namespace SshTunnelService.Workers;

public class TunnelWorker : BackgroundService
{
    private readonly ISshTunnelConfigRepository _configRepository;
    private readonly ISshTunnelOrchestrator _orchestrator;
    private readonly IFileLogger _logger;
    private readonly IEmailNotifier _emailNotifier;
    private readonly ConcurrentDictionary<int, Task> _tunnelTasks = new();

    public TunnelWorker(
        ISshTunnelConfigRepository configRepository,
        ISshTunnelOrchestrator orchestrator,
        IFileLogger logger,
        IEmailNotifier emailNotifier)
    {
        _configRepository = configRepository;
        _orchestrator = orchestrator;
        _logger = logger;
        _emailNotifier = emailNotifier;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _logger.LogAsync("INFO", "SSH Tunnel Worker started.");

        var configs = await _configRepository.GetAllActiveConfigsAsync(stoppingToken);

        if (configs.Count == 0)
        {
            await _logger.LogAsync("WARN", "No active tunnel configurations found. Worker idle.");
            return;
        }

        await _logger.LogAsync("INFO", $"Found {configs.Count} active tunnel(s). Launching...");

        // Spawn an independent reconnect loop per tunnel
        foreach (var config in configs)
        {
            var task = RunTunnelLoopAsync(config, stoppingToken);
            _tunnelTasks[config.Id] = task;
        }

        // Wait for all tunnel loops to complete (on cancellation)
        await Task.WhenAll(_tunnelTasks.Values);
        await _logger.LogAsync("INFO", "SSH Tunnel Worker stopped.");
    }

    private async Task RunTunnelLoopAsync(SshTunnelConfig config, CancellationToken stoppingToken)
    {
        var tunnelId = config.Id;
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_orchestrator.IsTunnelConnected(tunnelId))
                {
                    attempt++;
                    await _logger.LogAsync("INFO", $"[Tunnel #{tunnelId}] Connection attempt #{attempt}...");
                    await _orchestrator.RestartTunnelAsync(tunnelId, stoppingToken);
                    attempt = 0;
                }

                // Health-check loop for this tunnel
                while (_orchestrator.IsTunnelConnected(tunnelId) && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }

                if (!stoppingToken.IsCancellationRequested)
                {
                    await _logger.LogAsync("WARN", $"[Tunnel #{tunnelId}] Connection lost. Preparing to reconnect...");
                    await _emailNotifier.SendAsync(
                        $"Tunnel #{tunnelId} Connection Lost",
                        $"SSH tunnel #{tunnelId} connection was lost. Reconnecting...",
                        stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync("ERROR", $"[Tunnel #{tunnelId}] Attempt #{attempt} failed.", ex);

                if (config.MaxReconnectAttempts > 0 && attempt >= config.MaxReconnectAttempts)
                {
                    await _logger.LogAsync("FATAL", $"[Tunnel #{tunnelId}] Max reconnect attempts ({config.MaxReconnectAttempts}) reached.");
                    await _emailNotifier.SendAsync(
                        $"Tunnel #{tunnelId} Max Attempts Reached",
                        $"Tunnel #{tunnelId} failed after {config.MaxReconnectAttempts} attempts. Giving up.",
                        stoppingToken);
                    break;
                }
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await _logger.LogAsync("INFO", $"[Tunnel #{tunnelId}] Waiting {config.ReconnectDelaySeconds}s before reconnect...");
                await Task.Delay(TimeSpan.FromSeconds(config.ReconnectDelaySeconds), stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _logger.LogAsync("INFO", "SSH Tunnel Worker stopping...");
        await _orchestrator.StopAllAsync();
        await base.StopAsync(cancellationToken);
    }
}
