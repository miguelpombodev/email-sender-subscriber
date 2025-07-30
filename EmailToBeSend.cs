namespace SubEmailSender;

public class EmailToBeSend(string to, string subject, string body)
{
    public string To { get; set; } = to;
    public string Subject { get; set; } = subject;
    public string Body { get; set; } = body;
}