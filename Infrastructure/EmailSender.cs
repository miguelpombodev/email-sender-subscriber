using Cloudmart.Contracts.Messaging.Emails;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SubEmailSender.Config;
using SubEmailSender.Models;

namespace SubEmailSender.Infrastructure;

public interface IEmailSender
{
    Task SendEmailAsync(
        IEmailToBeSend email,
        CancellationToken cancellationToken = default);
}

public class EmailSender : IEmailSender
{
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(
        IOptions<SmtpOptions> smtpOptions,
        ILogger<EmailSender> logger)
    {
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
        
    }

    public async Task SendEmailAsync(
        IEmailToBeSend email,
        CancellationToken cancellationToken = default)
    {
        var message = CreateMimeMessage(email);

        using var client = new SmtpClient();

        await client.ConnectAsync(
            _smtpOptions.Host,
            _smtpOptions.Port,
            SecureSocketOptions.StartTls,
            cancellationToken);

        _logger.LogInformation(
            "Connected to SMTP host {Host}",
            _smtpOptions.Host);

        if (!string.IsNullOrWhiteSpace(_smtpOptions.User))
        {
            await client.AuthenticateAsync(
                _smtpOptions.User,
                _smtpOptions.Password,
                cancellationToken);

            _logger.LogInformation(
                "SMTP authentication successful");
        }

        await client.SendAsync(
            message,
            cancellationToken);

        _logger.LogInformation(
            "Email sent to {To} at {DateTime}",
            email.To,
            DateTime.UtcNow);

        await client.DisconnectAsync(
            true,
            cancellationToken);
    }

    private MimeMessage CreateMimeMessage(IEmailToBeSend email)
    {
        var message = new MimeMessage();

        message.From.Add(
            new MailboxAddress(
                _smtpOptions.FromName,
                _smtpOptions.FromEmail));

        message.To.Add(
            MailboxAddress.Parse(email.To));

        foreach (var cc in email.Cc)
        {
            message.Cc.Add(
                MailboxAddress.Parse(cc));
        }

        foreach (var bcc in email.Bcc)
        {
            message.Bcc.Add(
                MailboxAddress.Parse(bcc));
        }

        message.Subject = email.Subject;

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = email.IsBodyHtml
                ? email.Body
                : null,

            TextBody = email.IsBodyHtml
                ? null
                : email.Body
        };

        foreach (var attachment in email.Attachments)
        {
            var content = Convert.FromBase64String(
                attachment.ContentBase64);

            bodyBuilder.Attachments.Add(
                attachment.FileName,
                content,
                ContentType.Parse(attachment.ContentType));
        }

        message.Body = bodyBuilder.ToMessageBody();

        return message;
    }
}