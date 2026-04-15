using SshTunnelService.Services.Interfaces;

namespace SshTunnelService.Middleware;

public class GlobalExceptionHandler
{
    private readonly IFileLogger _logger;
    private readonly IEmailNotifier _emailNotifier;

    public GlobalExceptionHandler(IFileLogger logger, IEmailNotifier emailNotifier)
    {
        _logger = logger;
        _emailNotifier = emailNotifier;
    }

    public void Register()
    {
        AppDomain.CurrentDomain.UnhandledException += async (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            await _logger.LogAsync("FATAL", "Unhandled exception occurred.", ex);
            await _emailNotifier.SendAsync(
                "Fatal Error",
                $"An unhandled exception occurred:{Environment.NewLine}{ex?.Message}{Environment.NewLine}{ex?.StackTrace}");
        };

        TaskScheduler.UnobservedTaskException += async (_, args) =>
        {
            args.SetObserved();
            await _logger.LogAsync("ERROR", "Unobserved task exception.", args.Exception);
            await _emailNotifier.SendAsync(
                "Unobserved Task Error",
                $"An unobserved task exception occurred:{Environment.NewLine}{args.Exception?.Message}{Environment.NewLine}{args.Exception?.StackTrace}");
        };
    }
}
