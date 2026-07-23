using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BeerApi.Services;

// The app's first email dependency (#41) — password reset (#42) is the first real caller.
// Config-driven the same way CatalogBeerService.cs handles its API key: empty by default
// in the committed appsettings.json, real credentials only via an untracked .env-sourced
// environment variable, and sending silently no-ops (logs instead) when unconfigured
// rather than throwing, so local/dev runs and tests never need real SMTP creds.
public class SmtpEmailSender : IEmailSender
{
    private readonly ISmtpClientFactory _clientFactory;
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly string? _host;
    private readonly int _port;
    private readonly string? _username;
    private readonly string? _password;
    private readonly string? _fromAddress;
    private readonly string _fromName;
    private readonly bool _enableSsl;

    public SmtpEmailSender(ISmtpClientFactory clientFactory, IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
        _host = configuration["Email:SmtpHost"];
        _port = configuration.GetValue("Email:SmtpPort", 587);
        _username = configuration["Email:Username"];
        _password = configuration["Email:Password"];
        _fromAddress = configuration["Email:FromAddress"];
        _fromName = configuration["Email:FromName"] ?? "The Tavern";
        _enableSsl = configuration.GetValue("Email:EnableSsl", true);
    }

    public async Task SendAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_host) || string.IsNullOrWhiteSpace(_fromAddress))
        {
            _logger.LogInformation(
                "Email sending is not configured; skipping send to {ToEmail} (subject: {Subject}).",
                toEmail, subject);
            return;
        }

        using var client = _clientFactory.Create(_host, _port, _username, _password, _enableSsl);
        using var message = new MailMessage
        {
            From = new MailAddress(_fromAddress, _fromName),
            Subject = subject,
            Body = body,
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }
}
