namespace SshTunnelService.Services.Interfaces;

public interface IFileLogger
{
    Task LogAsync(string level, string message, Exception? exception = null);
    Task CleanupOldLogsAsync(CancellationToken cancellationToken = default);
}
