namespace SubEmailSender.Config;

public class SmtpOptions
{
    public string Host { get; set; } = "";
    public int Port { get; set; } = 25;
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "";
}