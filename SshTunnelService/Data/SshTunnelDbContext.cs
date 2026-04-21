using Microsoft.EntityFrameworkCore;
using SshTunnelService.Data.Entities;

namespace SshTunnelService.Data;

public class SshTunnelDbContext : DbContext
{
    public SshTunnelDbContext(DbContextOptions<SshTunnelDbContext> options) : base(options) { }

    public DbSet<SshTunnelEntity> SshTunnels => Set<SshTunnelEntity>();
    public DbSet<PortForwardEntity> PortForwards => Set<PortForwardEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SshTunnelEntity>(entity =>
        {
            entity.HasMany(e => e.PortForwards)
                  .WithOne(e => e.SshTunnel)
                  .HasForeignKey(e => e.SshTunnelId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PortForwardEntity>(entity =>
        {
            entity.HasIndex(e => new { e.SshTunnelId, e.ForwardType, e.BoundPort })
                  .HasDatabaseName("IX_SshTunnelPortForward_Tunnel_Type_Port");
        });
    }
}
