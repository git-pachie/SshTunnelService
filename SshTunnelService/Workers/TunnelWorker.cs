using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SshTunnelService.Helpers;
using SshTunnelService.Models;
using SshTunnelService.Services.Interfaces;

namespace SshTunnelService.Workers;

public class TunnelWorker : BackgroundService
{
    private readonly ISshTunnelManager _tunnelManager;
    private readonly IFileLogger _logger;
    private readonly IEmailNotifier _emailNotifier;
    private readonly SshTunnelConfig _config;

    public TunnelWorker(
        ISshTunnelManager tunnelManager,
        IFileLogger logger,
        IEmailNotifier emailNotifier,
        IOptions<SshTunnelConfig> config)
    {
        _tunnelManager = tunnelManager;
        _logger = logger;
        _emailNotifier = emailNotifier;
        _config = config.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _logger.LogAsync("INFO", "SSH Tunnel Worker started.");
        var attempt = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_tunnelManager.IsConnected)
                {
                    attempt++;
                    await _logger.LogAsync("INFO", $"Connection attempt #{attempt}...");
                    await _tunnelManager.ConnectAsync(stoppingToken);
                    attempt = 0; // reset on success
                }

                // Health-check loop
                while (_tunnelManager.IsConnected && !stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }

                // If we exit the inner loop, connection was lost
                if (!stoppingToken.IsCancellationRequested)
                {
                    await _logger.LogAsync("WARN", "SSH connection lost. Preparing to reconnect...");
                    await _emailNotifier.SendAsync(
                        "Connection Lost",
                        $"SSH tunnel connection to {LogMasker.MaskEndpoint(_config.Host, _config.Port)} was lost. Reconnecting...",
                        stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync("ERROR", $"Connection attempt #{attempt} failed.", ex);

                if (_config.MaxReconnectAttempts > 0 && attempt >= _config.MaxReconnectAttempts)
                {
                    await _logger.LogAsync("FATAL", $"Max reconnect attempts ({_config.MaxReconnectAttempts}) reached. Stopping.");
                    await _emailNotifier.SendAsync(
                        "Max Reconnect Attempts Reached",
                        $"Failed to reconnect after {_config.MaxReconnectAttempts} attempts. Service stopping.",
                        stoppingToken);
                    break;
                }
            }

            if (!stoppingToken.IsCancellationRequested)
            {
                await _logger.LogAsync("INFO", $"Waiting {_config.ReconnectDelaySeconds}s before reconnect...");
                await Task.Delay(TimeSpan.FromSeconds(_config.ReconnectDelaySeconds), stoppingToken);
            }
        }

        await _tunnelManager.DisconnectAsync();
        await _logger.LogAsync("INFO", "SSH Tunnel Worker stopped.");
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _logger.LogAsync("INFO", "SSH Tunnel Worker stopping...");
        await base.StopAsync(cancellationToken);
    }
}
