namespace SshTunnelService.Models;

public class SshTunnelConfig
{
    public int Id { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 22;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PrivateKeyPath { get; set; } = string.Empty;
    public string PrivateKeyPassphrase { get; set; } = string.Empty;
    public int ReconnectDelaySeconds { get; set; } = 10;
    public int MaxReconnectAttempts { get; set; } = 0; // 0 = unlimited
    public List<PortForwardEntry> LocalForwards { get; set; } = [];
    public List<PortForwardEntry> RemoteForwards { get; set; } = [];
    public List<DynamicForwardEntry> DynamicForwards { get; set; } = [];
}

public class PortForwardEntry
{
    public string Name { get; set; } = string.Empty;
    public string BoundHost { get; set; } = "127.0.0.1";
    public uint BoundPort { get; set; }
    public string TargetHost { get; set; } = "127.0.0.1";
    public uint TargetPort { get; set; }
}

public class DynamicForwardEntry
{
    public string Name { get; set; } = string.Empty;
    public string BoundHost { get; set; } = "127.0.0.1";
    public uint BoundPort { get; set; }
}
