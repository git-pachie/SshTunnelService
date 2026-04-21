using Renci.SshNet;
using SshTunnelService.Helpers;
using SshTunnelService.Models;
using SshTunnelService.Services.Interfaces;

namespace SshTunnelService.Services;

public class SshTunnelInstance : ISshTunnelInstance
{
    private readonly IFileLogger _logger;
    private readonly IEmailNotifier _emailNotifier;
    private SshClient? _sshClient;
    private SshTunnelConfig? _config;
    private readonly List<ForwardedPort> _forwardedPorts = [];

    public int TunnelId { get; private set; }
    public bool IsConnected => _sshClient?.IsConnected == true;

    private string MaskedEndpoint => _config is not null
        ? LogMasker.MaskEndpoint(_config.Host, _config.Port)
        : "***:***";

    public SshTunnelInstance(IFileLogger logger, IEmailNotifier emailNotifier)
    {
        _logger = logger;
        _emailNotifier = emailNotifier;
    }

    public async Task ConnectAsync(SshTunnelConfig config, CancellationToken cancellationToken)
    {
        _config = config;
        TunnelId = config.Id;

        var connectionInfo = BuildConnectionInfo();
        _sshClient = new SshClient(connectionInfo);

        await _logger.LogAsync("INFO", $"[Tunnel #{TunnelId}] Connecting to {MaskedEndpoint}...");
        _sshClient.Connect();

        if (!_sshClient.IsConnected)
            throw new InvalidOperationException($"[Tunnel #{TunnelId}] SSH connection failed.");

        await _logger.LogAsync("INFO", $"[Tunnel #{TunnelId}] Connected to {MaskedEndpoint}");
        SetupForwards();

        await _emailNotifier.SendAsync(
            $"Tunnel #{TunnelId} Connected",
            $"SSH tunnel #{TunnelId} connected to {MaskedEndpoint}",
            cancellationToken);
    }

    public async Task DisconnectAsync()
    {
        foreach (var port in _forwardedPorts)
        {
            try { port.Stop(); } catch { }
        }
        _forwardedPorts.Clear();

        if (_sshClient?.IsConnected == true)
        {
            _sshClient.Disconnect();
            await _logger.LogAsync("INFO", $"[Tunnel #{TunnelId}] Disconnected.");
            await _emailNotifier.SendAsync(
                $"Tunnel #{TunnelId} Disconnected",
                $"SSH tunnel #{TunnelId} disconnected from {MaskedEndpoint}");
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
        var config = _config!;
        var authMethods = new List<AuthenticationMethod>();

        if (!string.IsNullOrEmpty(config.PrivateKeyPath) && File.Exists(config.PrivateKeyPath))
        {
            var keyFile = string.IsNullOrEmpty(config.PrivateKeyPassphrase)
                ? new PrivateKeyFile(config.PrivateKeyPath)
                : new PrivateKeyFile(config.PrivateKeyPath, config.PrivateKeyPassphrase);
            authMethods.Add(new PrivateKeyAuthenticationMethod(config.Username, keyFile));
        }

        if (!string.IsNullOrEmpty(config.Password))
            authMethods.Add(new PasswordAuthenticationMethod(config.Username, config.Password));

        if (authMethods.Count == 0)
            throw new InvalidOperationException($"[Tunnel #{TunnelId}] No authentication method configured.");

        return new ConnectionInfo(config.Host, config.Port, config.Username, [.. authMethods]);
    }

    private void SetupForwards()
    {
        var config = _config!;

        foreach (var lf in config.LocalForwards)
        {
            var port = new ForwardedPortLocal(lf.BoundHost, lf.BoundPort, lf.TargetHost, lf.TargetPort);
            _sshClient!.AddForwardedPort(port);
            port.Start();
            _forwardedPorts.Add(port);
            _logger.LogAsync("INFO", $"[Tunnel #{TunnelId}] Local forward: {lf.BoundHost}:{lf.BoundPort} -> ***:*** ({lf.Name})").Wait();
        }

        foreach (var rf in config.RemoteForwards)
        {
            var boundHost = rf.BoundHost is "0.0.0.0" or "::" or "::0" ? string.Empty : rf.BoundHost;
            var port = new ForwardedPortRemote(boundHost, rf.BoundPort, rf.TargetHost, rf.TargetPort);
            _sshClient!.AddForwardedPort(port);
            port.Start();
            _forwardedPorts.Add(port);
            _logger.LogAsync("INFO", $"[Tunnel #{TunnelId}] Remote forward: ***:*** -> {rf.TargetHost}:{rf.TargetPort} ({rf.Name})").Wait();
        }

        foreach (var df in config.DynamicForwards)
        {
            var port = new ForwardedPortDynamic(df.BoundHost, df.BoundPort);
            _sshClient!.AddForwardedPort(port);
            port.Start();
            _forwardedPorts.Add(port);
            _logger.LogAsync("INFO", $"[Tunnel #{TunnelId}] Dynamic forward (SOCKS): {df.BoundHost}:{df.BoundPort} ({df.Name})").Wait();
        }
    }
}
