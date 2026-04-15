# SSH Tunnel Service 

A cross-platform .NET 9 console application that manages SSH port forwarding (local and remote), with auto-reconnect, file logging, and email notifications. Can run as a Windows Service or Linux systemd service.

## Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Setup User Secrets

This project uses [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) to store sensitive configuration outside of source control.

Initialize secrets (already done if you cloned this repo):

```bash
dotnet user-secrets init
```

Set all required secrets:

```bash
# SSH Tunnel
dotnet user-secrets set "SshTunnel:Host" "<ssh-server-hostname>"
dotnet user-secrets set "SshTunnel:Port" "<ssh-port>"
dotnet user-secrets set "SshTunnel:Username" "<ssh-username>"
dotnet user-secrets set "SshTunnel:Password" "<ssh-password>"
dotnet user-secrets set "SshTunnel:PrivateKeyPath" "<path-to-private-key>"
dotnet user-secrets set "SshTunnel:PrivateKeyPassphrase" "<private-key-passphrase>"
dotnet user-secrets set "SshTunnel:ReconnectDelaySeconds" "10"
dotnet user-secrets set "SshTunnel:MaxReconnectAttempts" "0"

# Local Port Forwards (repeat with index 0, 1, 2... for multiple)
dotnet user-secrets set "SshTunnel:LocalForwards:0:Name" "<forward-name>"
dotnet user-secrets set "SshTunnel:LocalForwards:0:BoundHost" "<local-bind-address>"
dotnet user-secrets set "SshTunnel:LocalForwards:0:BoundPort" "<local-bind-port>"
dotnet user-secrets set "SshTunnel:LocalForwards:0:TargetHost" "<remote-target-host>"
dotnet user-secrets set "SshTunnel:LocalForwards:0:TargetPort" "<remote-target-port>"

# Remote Port Forwards (repeat with index 0, 1, 2... for multiple)
dotnet user-secrets set "SshTunnel:RemoteForwards:0:Name" "<forward-name>"
dotnet user-secrets set "SshTunnel:RemoteForwards:0:BoundHost" "<remote-bind-address>"
dotnet user-secrets set "SshTunnel:RemoteForwards:0:BoundPort" "<remote-bind-port>"
dotnet user-secrets set "SshTunnel:RemoteForwards:0:TargetHost" "<local-target-host>"
dotnet user-secrets set "SshTunnel:RemoteForwards:0:TargetPort" "<local-target-port>"

# Email Notifications
dotnet user-secrets set "Email:Enabled" "<true-or-false>"
dotnet user-secrets set "Email:SmtpHost" "<smtp-server-hostname>"
dotnet user-secrets set "Email:SmtpPort" "<smtp-port>"
dotnet user-secrets set "Email:UseSsl" "<true-or-false>"
dotnet user-secrets set "Email:Username" "<smtp-username>"
dotnet user-secrets set "Email:Password" "<smtp-password>"
dotnet user-secrets set "Email:FromAddress" "<sender-email-address>"
dotnet user-secrets set "Email:FromName" "<sender-display-name>"
dotnet user-secrets set "Email:ToAddresses:0" "<recipient-email-address>"
dotnet user-secrets set "Email:SubjectPrefix" "<email-subject-prefix>"
```

### Run

```bash
dotnet run
```

### Install as Windows Service

```bash
dotnet publish -c Release -o ./publish
sc create SshTunnelService binPath="C:\path\to\publish\SshTunnelService.exe"
sc start SshTunnelService
```

### Install as Linux systemd Service

```bash
dotnet publish -c Release -o /opt/ssh-tunnel-service
```

Create `/etc/systemd/system/ssh-tunnel.service`:

```ini
[Unit]
Description=SSH Tunnel Service
After=network.target

[Service]
Type=notify
ExecStart=/opt/ssh-tunnel-service/SshTunnelService
Restart=always

[Install]
WantedBy=multi-user.target
```

```bash
sudo systemctl enable ssh-tunnel
sudo systemctl start ssh-tunnel
```
