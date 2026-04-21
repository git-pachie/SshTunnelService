using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SshTunnelService.Data;
using SshTunnelService.Models;
using SshTunnelService.Services.Interfaces;

namespace SshTunnelService.Services;

public class SshTunnelConfigRepository : ISshTunnelConfigRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SshTunnelConfigRepository(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<List<SshTunnelConfig>> GetAllActiveConfigsAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SshTunnelDbContext>();

        var entities = await db.SshTunnels
            .Include(t => t.PortForwards.Where(pf => pf.IsActive))
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToConfig).ToList();
    }

    public async Task<SshTunnelConfig?> GetConfigByIdAsync(int tunnelId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SshTunnelDbContext>();

        var entity = await db.SshTunnels
            .Include(t => t.PortForwards.Where(pf => pf.IsActive))
            .FirstOrDefaultAsync(t => t.Id == tunnelId && t.IsActive, cancellationToken);

        return entity is null ? null : MapToConfig(entity);
    }

    private static SshTunnelConfig MapToConfig(Data.Entities.SshTunnelEntity entity) => new()
    {
        Id = entity.Id,
        Host = entity.Host,
        Port = entity.Port,
        Username = entity.Username,
        Password = entity.Password,
        PrivateKeyPath = entity.PrivateKeyPath,
        PrivateKeyPassphrase = entity.PrivateKeyPassphrase,
        ReconnectDelaySeconds = entity.ReconnectDelaySeconds,
        MaxReconnectAttempts = entity.MaxReconnectAttempts,
        LocalForwards = entity.PortForwards
            .Where(pf => pf.ForwardType == "Local")
            .Select(pf => new PortForwardEntry
            {
                Name = pf.Name,
                BoundHost = pf.BoundHost,
                BoundPort = (uint)pf.BoundPort,
                TargetHost = pf.TargetHost ?? "127.0.0.1",
                TargetPort = (uint)(pf.TargetPort ?? 0)
            }).ToList(),
        RemoteForwards = entity.PortForwards
            .Where(pf => pf.ForwardType == "Remote")
            .Select(pf => new PortForwardEntry
            {
                Name = pf.Name,
                BoundHost = pf.BoundHost,
                BoundPort = (uint)pf.BoundPort,
                TargetHost = pf.TargetHost ?? "127.0.0.1",
                TargetPort = (uint)(pf.TargetPort ?? 0)
            }).ToList(),
        DynamicForwards = entity.PortForwards
            .Where(pf => pf.ForwardType == "Dynamic")
            .Select(pf => new DynamicForwardEntry
            {
                Name = pf.Name,
                BoundHost = pf.BoundHost,
                BoundPort = (uint)pf.BoundPort
            }).ToList()
    };
}
