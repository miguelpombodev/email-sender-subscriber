using Cloudmart.Contracts.Messaging.Interfaces.Emails;
using MassTransit;
using SubEmailSender.Models;

namespace SubEmailSender.Infrastructure;

public class EmailConsumer : IConsumer<IEmailToBeSend>
{
	private readonly IEmailSender _emailSender;

	private readonly ILogger<EmailConsumer> _logger;

	public EmailConsumer(
		IEmailSender emailSender,
		ILogger<EmailConsumer> logger)
	{
		_emailSender = emailSender;
		_logger = logger;
	}

	public async Task Consume(ConsumeContext<IEmailToBeSend> context)
	{
		var email = context.Message;

		_logger.LogInformation(
			"Processing email to {To}, MessageId: {MessageId}, EmailId: {EmailId}",
			email.To,
			context.MessageId,
			email.Id);

		await _emailSender.SendEmailAsync(
			email,
			context.CancellationToken);

		_logger.LogInformation(
			"Email successfully processed. To: {To}, MessageId: {MessageId}, EmailId: {EmailId}",
			email.To,
			context.MessageId,
			email.Id);
	}
}
