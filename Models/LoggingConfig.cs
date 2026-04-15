namespace SshTunnelService.Models;

public class LoggingConfig
{
    public string LogDirectory { get; set; } = "logs";
    public string FileNamePattern { get; set; } = "ssh-tunnel-{date}.log";
    public int RetainDays { get; set; } = 30;
}
