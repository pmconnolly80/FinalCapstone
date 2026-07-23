using System.Net;
using System.Net.Mail;

namespace BeerApi.Services;

public class SmtpClientFactory : ISmtpClientFactory
{
    public ISmtpClient Create(string host, int port, string? username, string? password, bool enableSsl)
    {
        var client = new SmtpClient(host, port) { EnableSsl = enableSsl };
        if (!string.IsNullOrWhiteSpace(username))
        {
            client.Credentials = new NetworkCredential(username, password);
        }

        return new SmtpClientAdapter(client);
    }

    private sealed class SmtpClientAdapter : ISmtpClient
    {
        private readonly SmtpClient _client;

        public SmtpClientAdapter(SmtpClient client)
        {
            _client = client;
        }

        public Task SendMailAsync(MailMessage message) => _client.SendMailAsync(message);

        public void Dispose() => _client.Dispose();
    }
}
