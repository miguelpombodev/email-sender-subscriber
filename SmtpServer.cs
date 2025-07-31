namespace SubEmailSender;

public class SmtpServer
{
    public SmtpServer(string host, string user, string password, string fromEmail, string fromName, int port)
    {
        Host = host;
        User = user;
        Password = password;
        FromEmail = fromEmail;
        FromName = fromName;
        Port = port;
    }

    public string Host { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
    public int Port { get; set; }
}