using SshTunnelService.Middleware;
using SshTunnelService.Models;
using SshTunnelService.Services;
using SshTunnelService.Services.Interfaces;
using SshTunnelService.Workers;
using Microsoft.Extensions.Configuration;

var builder = Host.CreateApplicationBuilder(args);

// Always load user secrets regardless of environment
builder.Configuration.AddUserSecrets<Program>();

// Bind configuration sections
builder.Services.Configure<SshTunnelConfig>(builder.Configuration.GetSection("SshTunnel"));
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<LoggingConfig>(builder.Configuration.GetSection("FileLogging"));

// Register services — swap implementations here for modularity
builder.Services.AddSingleton<IFileLogger, FileLogger>();
builder.Services.AddSingleton<IEmailNotifier, EmailNotifier>();
builder.Services.AddSingleton<ISshTunnelManager, SshTunnelManager>();
builder.Services.AddSingleton<GlobalExceptionHandler>();

// Background worker
builder.Services.AddHostedService<TunnelWorker>();

// Cross-platform service support
builder.Services.AddWindowsService(options => options.ServiceName = "MySshTunnelService");
builder.Services.AddSystemd();

var host = builder.Build();

// Wire up global exception handler
var exceptionHandler = host.Services.GetRequiredService<GlobalExceptionHandler>();
exceptionHandler.Register();

host.Run();
