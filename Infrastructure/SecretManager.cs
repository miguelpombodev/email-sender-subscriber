namespace SubEmailSender.Infrastructure;

public class SecretManager
{
    public SecretManager(string? clientId, string? clientSecret, string? projectId, string environment = "dev",string secretPath = "/")
    {
        ClientId = clientId ?? throw new ArgumentNullException("ClientId not configured!");
        ClientSecret = clientSecret ?? throw new ArgumentNullException("ClientSecret not configured!");
        ProjectId = projectId ?? throw new ArgumentNullException("ProjectId not configured!");
        Environment = environment;
        SecretPath = secretPath;
    }

    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string ProjectId { get; set; }
    public string Environment { get; set; }
    public string SecretPath { get; set; }
}