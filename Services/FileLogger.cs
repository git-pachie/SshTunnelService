using Microsoft.Extensions.Options;
using SshTunnelService.Models;
using SshTunnelService.Services.Interfaces;

namespace SshTunnelService.Services;

public class FileLogger : IFileLogger
{
    private readonly LoggingConfig _config;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public FileLogger(IOptions<LoggingConfig> config)
    {
        _config = config.Value;
        Directory.CreateDirectory(_config.LogDirectory);
    }

    public async Task LogAsync(string level, string message, Exception? exception = null)
    {
        var fileName = _config.FileNamePattern.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"));
        var filePath = Path.Combine(_config.LogDirectory, fileName);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logLine = $"[{timestamp}] [{level}] {message}";

        if (exception is not null)
            logLine += $"{Environment.NewLine}  Exception: {exception.GetType().Name}: {exception.Message}"
                     + $"{Environment.NewLine}  StackTrace: {exception.StackTrace}";

        logLine += Environment.NewLine;

        await _writeLock.WaitAsync();
        try
        {
            await File.AppendAllTextAsync(filePath, logLine);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public Task CleanupOldLogsAsync(CancellationToken cancellationToken = default)
    {
        if (_config.RetainDays <= 0) return Task.CompletedTask;

        var cutoff = DateTime.Now.AddDays(-_config.RetainDays);
        try
        {
            var logDir = new DirectoryInfo(_config.LogDirectory);
            if (!logDir.Exists) return Task.CompletedTask;

            foreach (var file in logDir.GetFiles("*.log"))
            {
                if (cancellationToken.IsCancellationRequested) break;
                if (file.LastWriteTime < cutoff)
                    file.Delete();
            }
        }
        catch
        {
            // Swallow cleanup errors — non-critical
        }

        return Task.CompletedTask;
    }
}
