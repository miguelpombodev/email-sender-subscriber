using MassTransit;
using SubEmailSender.Models;

namespace SubEmailSender.Infrastructure;

public class EmailConsumer : IConsumer<EmailToBeSend>
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

	public async Task Consume(ConsumeContext<EmailToBeSend> context)
	{
		var email = context.Message;

		_logger.LogInformation(
			"Processing email to {To}, MessageId: {MessageId}",
			email.To,
			context.MessageId);

		await _emailSender.SendEmailAsync(
			email,
			context.CancellationToken);

		_logger.LogInformation(
			"Email successfully processed. To: {To}, MessageId: {MessageId}",
			email.To,
			context.MessageId);
	}
}
