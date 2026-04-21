using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SshTunnelService.Data.Entities;

[Table("SshTunnel")]
public class SshTunnelEntity
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(255)]
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 22;

    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(500)]
    public string PrivateKeyPath { get; set; } = string.Empty;

    [MaxLength(255)]
    public string PrivateKeyPassphrase { get; set; } = string.Empty;

    public int ReconnectDelaySeconds { get; set; } = 10;

    public int MaxReconnectAttempts { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<PortForwardEntity> PortForwards { get; set; } = [];
}
