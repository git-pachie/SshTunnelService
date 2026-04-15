namespace SshTunnelService.Services.Interfaces;

public interface ISshTunnelManager : IDisposable
{
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken);
    Task DisconnectAsync();
}
