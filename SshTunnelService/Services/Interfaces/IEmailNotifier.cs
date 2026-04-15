namespace SshTunnelService.Services.Interfaces;

public interface IEmailNotifier
{
    Task SendAsync(string subject, string body, CancellationToken cancellationToken = default);
}
