using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using SshTunnelService.Models;
using SshTunnelService.Services.Interfaces;

namespace SshTunnelService.Services;

public class EmailNotifier : IEmailNotifier
{
    private readonly EmailConfig _config;
    private readonly IFileLogger _logger;

    public EmailNotifier(IOptions<EmailConfig> config, IFileLogger logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task SendAsync(string subject, string body, CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            await _logger.LogAsync("INFO", $"Email notification skipped (disabled): {subject}");
            return;
        }

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_config.FromName, _config.FromAddress));

            foreach (var to in _config.ToAddresses)
                message.To.Add(MailboxAddress.Parse(to));

            message.Subject = $"{_config.SubjectPrefix} {subject}";
            message.Body = new TextPart("plain") { Text = body };

            using var client = new SmtpClient();
            await client.ConnectAsync(_config.SmtpHost, _config.SmtpPort, _config.UseSsl, cancellationToken);

            if (!string.IsNullOrEmpty(_config.Username))
                await client.AuthenticateAsync(_config.Username, _config.Password, cancellationToken);

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            await _logger.LogAsync("INFO", $"Email sent: {subject}");
        }
        catch (Exception ex)
        {
            await _logger.LogAsync("ERROR", $"Failed to send email: {subject}", ex);
        }
    }
}
