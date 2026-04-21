using System.Collections.Concurrent;
using SshTunnelService.Services.Interfaces;

namespace SshTunnelService.Services;

public class SshTunnelOrchestrator : ISshTunnelOrchestrator
{
    private readonly ISshTunnelConfigRepository _configRepository;
    private readonly IFileLogger _logger;
    private readonly IEmailNotifier _emailNotifier;
    private readonly ConcurrentDictionary<int, ISshTunnelInstance> _instances = new();

    public SshTunnelOrchestrator(
        ISshTunnelConfigRepository configRepository,
        IFileLogger logger,
        IEmailNotifier emailNotifier)
    {
        _configRepository = configRepository;
        _logger = logger;
        _emailNotifier = emailNotifier;
    }

    public async Task StartAllAsync(CancellationToken cancellationToken)
    {
        var configs = await _configRepository.GetAllActiveConfigsAsync(cancellationToken);

        if (configs.Count == 0)
        {
            await _logger.LogAsync("WARN", "No active SSH tunnel configurations found in database.");
            return;
        }

        await _logger.LogAsync("INFO", $"Starting {configs.Count} tunnel(s)...");

        foreach (var config in configs)
        {
            await StartTunnelAsync(config.Id, cancellationToken);
        }
    }

    public async Task StopAllAsync()
    {
        await _logger.LogAsync("INFO", "Stopping all tunnels...");

        foreach (var kvp in _instances)
        {
            await StopTunnelInstanceAsync(kvp.Value);
        }
        _instances.Clear();
    }

    public async Task RestartTunnelAsync(int tunnelId, CancellationToken cancellationToken)
    {
        await _logger.LogAsync("INFO", $"[Tunnel #{tunnelId}] Restarting...");

        // Stop existing instance if running
        if (_instances.TryRemove(tunnelId, out var existing))
        {
            await StopTunnelInstanceAsync(existing);
        }

        await StartTunnelAsync(tunnelId, cancellationToken);
    }

    public bool IsTunnelConnected(int tunnelId)
        => _instances.TryGetValue(tunnelId, out var instance) && instance.IsConnected;

    public IReadOnlyDictionary<int, bool> GetTunnelStatuses()
        => _instances.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.IsConnected);

    private async Task StartTunnelAsync(int tunnelId, CancellationToken cancellationToken)
    {
        var config = await _configRepository.GetConfigByIdAsync(tunnelId, cancellationToken);
        if (config is null)
        {
            await _logger.LogAsync("WARN", $"[Tunnel #{tunnelId}] Config not found or inactive. Skipping.");
            return;
        }

        var instance = new SshTunnelInstance(_logger, _emailNotifier);

        try
        {
            await instance.ConnectAsync(config, cancellationToken);
            _instances[tunnelId] = instance;
        }
        catch (Exception ex)
        {
            await _logger.LogAsync("ERROR", $"[Tunnel #{tunnelId}] Failed to start.", ex);
            instance.Dispose();
        }
    }

    private async Task StopTunnelInstanceAsync(ISshTunnelInstance instance)
    {
        try
        {
            await instance.DisconnectAsync();
        }
        catch (Exception ex)
        {
            await _logger.LogAsync("ERROR", $"[Tunnel #{instance.TunnelId}] Error during disconnect.", ex);
        }
        finally
        {
            instance.Dispose();
        }
    }
}
