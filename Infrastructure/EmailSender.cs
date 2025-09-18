using MimeKit;
using MailKit.Net.Smtp;
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
    public EmailSender(SmtpOptions smtpOptions, ILogger<EmailSender> logger)
    {
        _smtpOptions = smtpOptions;
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
            _logger.LogInformation("Conectado ao host SMTP.");

            if (!string.IsNullOrWhiteSpace(_smtpOptions.User))
            {
                await client.AuthenticateAsync(_smtpOptions.User, _smtpOptions.Password);
                _logger.LogInformation("Autenticado com sucesso.");
            }

            await client.SendAsync(message);
            _logger.LogInformation("E-mail enviado para {To}.", email.To);

            await client.DisconnectAsync(true);
            
            _logger.LogInformation("Email sent to {To}", email.To);
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

            _logger.LogInformation("Email message built");

            return message;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}