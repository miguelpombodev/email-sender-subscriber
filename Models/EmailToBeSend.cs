namespace SubEmailSender.Models;

public class EmailToBeSend
{
    public string To { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Body { get; set; } = "";
}