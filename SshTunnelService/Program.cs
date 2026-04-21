using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SshTunnelService.Data;
using SshTunnelService.Middleware;
using SshTunnelService.Models;
using SshTunnelService.Services;
using SshTunnelService.Services.Interfaces;
using SshTunnelService.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Always load user secrets regardless of environment
builder.Configuration.AddUserSecrets<Program>();

// Bind configuration sections (Email & Logging stay in appsettings/secrets)
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("Email"));
builder.Services.Configure<LoggingConfig>(builder.Configuration.GetSection("FileLogging"));

// Database context — SSH tunnel config is stored in MSSQL
builder.Services.AddDbContext<SshTunnelDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SshTunnelDb")));

// Register services — swap implementations here for modularity
builder.Services.AddSingleton<IFileLogger, FileLogger>();
builder.Services.AddSingleton<IEmailNotifier, EmailNotifier>();
builder.Services.AddSingleton<ISshTunnelConfigRepository, SshTunnelConfigRepository>();
builder.Services.AddSingleton<ISshTunnelOrchestrator, SshTunnelOrchestrator>();
builder.Services.AddSingleton<GlobalExceptionHandler>();

// Background worker
builder.Services.AddHostedService<TunnelWorker>();

// Cross-platform service support
builder.Services.AddWindowsService(options => options.ServiceName = "SshTunnelService");
builder.Services.AddSystemd();

var host = builder.Build();

// Wire up global exception handler
var exceptionHandler = host.Services.GetRequiredService<GlobalExceptionHandler>();
exceptionHandler.Register();

host.Run();
