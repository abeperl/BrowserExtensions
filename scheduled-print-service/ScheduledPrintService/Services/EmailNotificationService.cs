using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScheduledPrintService.Models;

namespace ScheduledPrintService.Services;

public interface IEmailNotificationService
{
    Task TrySendAsync(string subject, string body, CancellationToken ct);
}

public class EmailNotificationService : IEmailNotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;
    private readonly EmailConfig _config;

    public EmailNotificationService(ILogger<EmailNotificationService> logger, IOptions<EmailConfig> options)
    {
        _logger = logger;
        _config = options.Value;
    }

    public async Task TrySendAsync(string subject, string body, CancellationToken ct)
    {
        if (!_config.Enabled)
        {
            _logger.LogDebug("Email disabled: {Subject}", subject);
            return;
        }

        try
        {
            using var client = new SmtpClient(_config.SmtpHost, _config.SmtpPort)
            {
                EnableSsl = _config.UseSsl
            };

            if (!string.IsNullOrWhiteSpace(_config.Username) && _config.Password != null)
            {
                client.Credentials = new NetworkCredential(_config.Username, _config.Password);
            }

            using var msg = new MailMessage()
            {
                From = new MailAddress(_config.From),
                Subject = subject,
                Body = body
            };

            foreach (var addr in _config.To.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                msg.To.Add(addr);
            }

            await client.SendMailAsync(msg, ct);
            _logger.LogInformation("Email sent: {Subject}", subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email: {Subject}", subject);
        }
    }
}
