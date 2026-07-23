using System.Net.Mail;

namespace BeerApi.Services;

// Thin seam over System.Net.Mail.SmtpClient so SmtpEmailSender's send path can be unit
// tested without touching a real network socket.
public interface ISmtpClient : IDisposable
{
    Task SendMailAsync(MailMessage message);
}

public interface ISmtpClientFactory
{
    ISmtpClient Create(string host, int port, string? username, string? password, bool enableSsl);
}
