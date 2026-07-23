using System.Net.Mail;
using BeerApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BeerApi.Tests.Services;

public class SmtpEmailSenderTests
{
    private class FakeSmtpClient : ISmtpClient
    {
        public MailMessage? SentMessage { get; private set; }

        public Task SendMailAsync(MailMessage message)
        {
            SentMessage = message;
            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }

    private class FakeSmtpClientFactory : ISmtpClientFactory
    {
        public int CallCount { get; private set; }
        public string? Host { get; private set; }
        public int Port { get; private set; }
        public string? Username { get; private set; }
        public string? Password { get; private set; }
        public bool EnableSsl { get; private set; }
        public FakeSmtpClient Client { get; } = new();

        public ISmtpClient Create(string host, int port, string? username, string? password, bool enableSsl)
        {
            CallCount++;
            Host = host;
            Port = port;
            Username = username;
            Password = password;
            EnableSsl = enableSsl;
            return Client;
        }
    }

    private static (SmtpEmailSender Sender, FakeSmtpClientFactory Factory) CreateSender(
        Dictionary<string, string?>? config = null)
    {
        var factory = new FakeSmtpClientFactory();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config ?? new Dictionary<string, string?>())
            .Build();
        var sender = new SmtpEmailSender(factory, configuration, NullLogger<SmtpEmailSender>.Instance);
        return (sender, factory);
    }

    [Fact]
    public async Task SendAsync_Unconfigured_DoesNotCallSmtpClient()
    {
        var (sender, factory) = CreateSender();

        await sender.SendAsync("customer@example.com", "Subject", "Body");

        Assert.Equal(0, factory.CallCount);
    }

    [Fact]
    public async Task SendAsync_MissingFromAddress_DoesNotCallSmtpClient()
    {
        var (sender, factory) = CreateSender(new Dictionary<string, string?>
        {
            ["Email:SmtpHost"] = "smtp.example.com",
        });

        await sender.SendAsync("customer@example.com", "Subject", "Body");

        Assert.Equal(0, factory.CallCount);
    }

    [Fact]
    public async Task SendAsync_Configured_SendsMailWithExpectedFields()
    {
        var (sender, factory) = CreateSender(new Dictionary<string, string?>
        {
            ["Email:SmtpHost"] = "smtp.example.com",
            ["Email:SmtpPort"] = "2525",
            ["Email:Username"] = "smtp-user",
            ["Email:Password"] = "smtp-pass",
            ["Email:FromAddress"] = "tavern@example.com",
            ["Email:FromName"] = "The Tavern",
            ["Email:EnableSsl"] = "false",
        });

        await sender.SendAsync("customer@example.com", "Reset your password", "Click here");

        Assert.Equal(1, factory.CallCount);
        Assert.Equal("smtp.example.com", factory.Host);
        Assert.Equal(2525, factory.Port);
        Assert.Equal("smtp-user", factory.Username);
        Assert.Equal("smtp-pass", factory.Password);
        Assert.False(factory.EnableSsl);

        var sent = factory.Client.SentMessage;
        Assert.NotNull(sent);
        Assert.Equal("tavern@example.com", sent!.From!.Address);
        Assert.Equal("The Tavern", sent.From!.DisplayName);
        Assert.Equal("customer@example.com", sent.To.Single().Address);
        Assert.Equal("Reset your password", sent.Subject);
        Assert.Equal("Click here", sent.Body);
    }
}
