using SshTunnelService.Models;

namespace SshTunnelService.Services.Interfaces;

public interface ISshTunnelInstance : IDisposable
{
    int TunnelId { get; }
    bool IsConnected { get; }
    Task ConnectAsync(SshTunnelConfig config, CancellationToken cancellationToken);
    Task DisconnectAsync();
}

public interface ISshTunnelOrchestrator
{
    Task StartAllAsync(CancellationToken cancellationToken);
    Task StopAllAsync();
    Task RestartTunnelAsync(int tunnelId, CancellationToken cancellationToken);
    bool IsTunnelConnected(int tunnelId);
}
