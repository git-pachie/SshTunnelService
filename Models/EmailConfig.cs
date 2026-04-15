namespace SshTunnelService.Models;

public class EmailConfig
{
    public bool Enabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "SSH Tunnel Service";
    public List<string> ToAddresses { get; set; } = [];
    public string SubjectPrefix { get; set; } = "[SSH Tunnel]";
}
