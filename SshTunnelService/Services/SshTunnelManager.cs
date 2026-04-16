using Microsoft.Extensions.Options;
using Renci.SshNet;
using SshTunnelService.Models;
using SshTunnelService.Services.Interfaces;

namespace SshTunnelService.Services;

public class SshTunnelManager : ISshTunnelManager
{
    private readonly SshTunnelConfig _config;
    private readonly IFileLogger _logger;
    private readonly IEmailNotifier _emailNotifier;
    private SshClient? _sshClient;
    private readonly List<ForwardedPort> _forwardedPorts = [];

    public bool IsConnected => _sshClient?.IsConnected == true;

    public SshTunnelManager(
        IOptions<SshTunnelConfig> config,
        IFileLogger logger,
        IEmailNotifier emailNotifier)
    {
        _config = config.Value;
        _logger = logger;
        _emailNotifier = emailNotifier;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        var connectionInfo = BuildConnectionInfo();
        _sshClient = new SshClient(connectionInfo);

        await _logger.LogAsync("INFO", $"Connecting to {_config.Host}:{_config.Port}...");
        _sshClient.Connect();

        if (!_sshClient.IsConnected)
            throw new InvalidOperationException("SSH connection failed.");

        await _logger.LogAsync("INFO", $"Connected to {_config.Host}:{_config.Port}");

        SetupForwards();

        var summary = GetForwardSummary();
        await _emailNotifier.SendAsync(
            "Connected",
            $"SSH tunnel connected to {_config.Host}:{_config.Port}{Environment.NewLine}{summary}",
            cancellationToken);
    }

    public async Task DisconnectAsync()
    {
        foreach (var port in _forwardedPorts)
        {
            try { port.Stop(); } catch { /* best effort */ }
        }
        _forwardedPorts.Clear();

        if (_sshClient?.IsConnected == true)
        {
            _sshClient.Disconnect();
            await _logger.LogAsync("INFO", "SSH tunnel disconnected.");
            await _emailNotifier.SendAsync(
                "Disconnected",
                $"SSH tunnel disconnected from {_config.Host}:{_config.Port}");
        }
    }

    public void Dispose()
    {
        foreach (var port in _forwardedPorts)
        {
            try { port.Stop(); port.Dispose(); } catch { }
        }
        _forwardedPorts.Clear();
        _sshClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    private ConnectionInfo BuildConnectionInfo()
    {
        var authMethods = new List<AuthenticationMethod>();

        if (!string.IsNullOrEmpty(_config.PrivateKeyPath) && File.Exists(_config.PrivateKeyPath))
        {
            var keyFile = string.IsNullOrEmpty(_config.PrivateKeyPassphrase)
                ? new PrivateKeyFile(_config.PrivateKeyPath)
                : new PrivateKeyFile(_config.PrivateKeyPath, _config.PrivateKeyPassphrase);
            authMethods.Add(new PrivateKeyAuthenticationMethod(_config.Username, keyFile));
        }

        if (!string.IsNullOrEmpty(_config.Password))
            authMethods.Add(new PasswordAuthenticationMethod(_config.Username, _config.Password));

        if (authMethods.Count == 0)
            throw new InvalidOperationException("No authentication method configured. Provide a password or private key.");

        return new ConnectionInfo(_config.Host, _config.Port, _config.Username, [.. authMethods]);
    }

    private void SetupForwards()
    {
        foreach (var lf in _config.LocalForwards)
        {
            var port = new ForwardedPortLocal(lf.BoundHost, lf.BoundPort, lf.TargetHost, lf.TargetPort);
            _sshClient!.AddForwardedPort(port);
            port.Start();
            _forwardedPorts.Add(port);
            _logger.LogAsync("INFO", $"Local forward: {lf.BoundHost}:{lf.BoundPort} -> {lf.TargetHost}:{lf.TargetPort} ({lf.Name})").Wait();
        }

        foreach (var rf in _config.RemoteForwards)
        {
            // SSH.NET requires empty string (not 0.0.0.0) to bind on all interfaces
            var boundHost = rf.BoundHost is "0.0.0.0" or "::" or "::0" ? string.Empty : rf.BoundHost;
            var port = new ForwardedPortRemote(boundHost, rf.BoundPort, rf.TargetHost, rf.TargetPort);
            _sshClient!.AddForwardedPort(port);
            port.Start();
            _forwardedPorts.Add(port);
            _logger.LogAsync("INFO", $"Remote forward: {rf.BoundHost}:{rf.BoundPort} -> {rf.TargetHost}:{rf.TargetPort} ({rf.Name})").Wait();
        }

        foreach (var df in _config.DynamicForwards)
        {
            var port = new ForwardedPortDynamic(df.BoundHost, df.BoundPort);
            _sshClient!.AddForwardedPort(port);
            port.Start();
            _forwardedPorts.Add(port);
            _logger.LogAsync("INFO", $"Dynamic forward (SOCKS): {df.BoundHost}:{df.BoundPort} ({df.Name})").Wait();
        }
    }

    private string GetForwardSummary()
    {
        var lines = new List<string>();
        foreach (var lf in _config.LocalForwards)
            lines.Add($"  [Local]  {lf.BoundHost}:{lf.BoundPort} -> {lf.TargetHost}:{lf.TargetPort} ({lf.Name})");
        foreach (var rf in _config.RemoteForwards)
            lines.Add($"  [Remote]  {rf.BoundHost}:{rf.BoundPort} -> {rf.TargetHost}:{rf.TargetPort} ({rf.Name})");
        foreach (var df in _config.DynamicForwards)
            lines.Add($"  [Dynamic] {df.BoundHost}:{df.BoundPort} SOCKS proxy ({df.Name})");
        return lines.Count > 0 ? string.Join(Environment.NewLine, lines) : "  No port forwards configured.";
    }
}
