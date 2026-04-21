using SshTunnelService.Models;

namespace SshTunnelService.Services.Interfaces;

public interface ISshTunnelConfigRepository
{
    Task<List<SshTunnelConfig>> GetAllActiveConfigsAsync(CancellationToken cancellationToken = default);
    Task<SshTunnelConfig?> GetConfigByIdAsync(int tunnelId, CancellationToken cancellationToken = default);
}
