using Cloudmart.Contracts.Messaging.Emails;

namespace SubEmailSender.Models;

public record EmailToBeSendContract : IEmailToBeSend
{
	public Guid Id { get; set; }
	public string To { get; init; } = string.Empty;

	public List<string> Cc { get; init; } = [];

	public List<string> Bcc { get; init; } = [];

	public string Subject { get; init; } = string.Empty;

	public string Body { get; init; } = string.Empty;

	public bool IsBodyHtml { get; init; } = true;

	public List<IEmailAttachment> Attachments { get; init; } = [];
}

public record EmailAttachment : IEmailAttachment
{
	public string FileName { get; init; } = string.Empty;

	public string ContentType { get; init; } = "application/octet-stream";

	public string ContentBase64 { get; init; } = string.Empty;
}