namespace SubEmailSender.Infrastructure;

public class SecretManager
{
    public SecretManager(string? clientId, string? clientSecret, string? projectId, string environment = "dev",
        string secretPath = "/")
    {
        ClientId = string.IsNullOrEmpty(clientId)
            ? throw new ArgumentNullException("ClientId not configured!")
            : clientId;
        ClientSecret = string.IsNullOrEmpty(clientSecret)
            ? throw new ArgumentNullException("ClientSecret not configured!")
            : clientSecret;
        ProjectId = string.IsNullOrEmpty(projectId)
            ? throw new ArgumentNullException("ProjectId not configured!")
            : projectId;
        Environment = environment;
        SecretPath = secretPath;
    }

    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string ProjectId { get; set; }
    public string Environment { get; set; }
    public string SecretPath { get; set; }
}