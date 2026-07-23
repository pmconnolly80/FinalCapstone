using System.Collections.Concurrent;
using BeerApi.Services;

namespace BeerApi.Tests.TestDoubles;

public class FakeEmailSender : IEmailSender
{
    public record SentEmail(string ToEmail, string Subject, string Body);

    private readonly ConcurrentQueue<SentEmail> _sent = new();

    public IReadOnlyCollection<SentEmail> SentEmails => _sent.ToArray();

    public Task SendAsync(string toEmail, string subject, string body)
    {
        _sent.Enqueue(new SentEmail(toEmail, subject, body));
        return Task.CompletedTask;
    }
}
