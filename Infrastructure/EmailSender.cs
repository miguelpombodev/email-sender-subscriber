using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using SubEmailSender.Config;
using SubEmailSender.Models;

namespace SubEmailSender.Infrastructure;

public interface IEmailSender
{
    Task SendEmailAsync(EmailToBeSend email, CancellationToken cancellationToken = default);
}

public class EmailSender : IEmailSender
{
    public EmailSender(IOptions<SmtpOptions> smtpOptions, ILogger<EmailSender> logger)
    {
        _smtpOptions = smtpOptions.Value;
        _logger = logger;

        _retryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3,
            attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (ex, ts, attempt, context) =>
            {
                _logger.LogWarning(ex, "Retry {Attempt} to send email after {Delay}", attempt, ts);
            }
        );
    }

    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<EmailSender> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public async Task SendEmailAsync(EmailToBeSend email, CancellationToken cancellationToken = default)
    {
        var message = CreateMimeMessage(email);


        await _retryPolicy.ExecuteAsync(async ct =>
        {
            using var client = new SmtpClient();

            await client.ConnectAsync(_smtpOptions.Host, _smtpOptions.Port, useSsl: false);
            _logger.LogInformation("Connected successfully to STMP Host.");

            if (!string.IsNullOrWhiteSpace(_smtpOptions.User))
            {
                await client.AuthenticateAsync(_smtpOptions.User, _smtpOptions.Password);
                _logger.LogInformation("Authenticated successfully!");
            }

            await client.SendAsync(message);
            _logger.LogInformation("E-mail sent to {To} at {Datetime}.", email.To, DateTime.UtcNow);

            await client.DisconnectAsync(true);
        }, cancellationToken);
    }

    private MimeMessage CreateMimeMessage(EmailToBeSend email)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpOptions.FromName, _smtpOptions.FromEmail));
            message.To.Add(new MailboxAddress(email.To, email.To));
            message.Subject = email.Subject;

            message.Body = new BodyBuilder
            {
                HtmlBody = email.Body
            }.ToMessageBody();

            _logger.LogInformation("Email message built - {EmailJsonBody}", JsonConvert.SerializeObject(email));

            return message;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}