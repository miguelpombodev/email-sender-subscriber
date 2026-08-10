namespace SubEmailSender.Models;

public record EmailToBeSend
{
	public Guid EmailId { get; set; }
	public string To { get; init; } = string.Empty;

	public List<string> Cc { get; init; } = [];

	public List<string> Bcc { get; init; } = [];

	public string Subject { get; init; } = string.Empty;

	public string Body { get; init; } = string.Empty;

	public bool IsBodyHtml { get; init; } = true;

	public List<EmailAttachment> Attachments { get; init; } = [];
}

public record EmailAttachment
{
	public string FileName { get; init; } = string.Empty;

	public string ContentType { get; init; } = "application/octet-stream";

	public string ContentBase64 { get; init; } = string.Empty;
}
