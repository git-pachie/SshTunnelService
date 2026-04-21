using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SshTunnelService.Data.Entities;

[Table("SshTunnelPortForward")]
public class PortForwardEntity
{
    [Key]
    public int Id { get; set; }

    public int SshTunnelId { get; set; }

    [Required, MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// "Local", "Remote", or "Dynamic"
    /// </summary>
    [Required, MaxLength(10)]
    public string ForwardType { get; set; } = "Local";

    [MaxLength(255)]
    public string BoundHost { get; set; } = "127.0.0.1";

    public int BoundPort { get; set; }

    [MaxLength(255)]
    public string? TargetHost { get; set; } = "127.0.0.1";

    public int? TargetPort { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(SshTunnelId))]
    public SshTunnelEntity SshTunnel { get; set; } = null!;
}
