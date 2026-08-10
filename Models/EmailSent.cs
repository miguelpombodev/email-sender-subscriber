namespace SubEmailSender.Models;

public sealed class EmailSent
{
	public Guid EmailId { get; set; }
	public DateTime SentAt { get; set; }
}
