using Infisical.Sdk;
using Infisical.Sdk.Model;
using Serilog;
using SubEmailSender.Config;
using SubEmailSender.Infrastructure;

namespace SubEmailSender;

public static class Program
{
    private static readonly SecretManager SecretManager = new(
        Environment.GetEnvironmentVariable("INFISICAL_CLIENT_ID"),
        Environment.GetEnvironmentVariable("INFISICAL_CLIENT_SECRET"),
        Environment.GetEnvironmentVariable("INFISICAL_PROJECT_ID")
    );
    public static async Task Main(string[] args)
    {
       
       
        var infisicalSecrets = await FetchSecretsFromInfisical();

        var builder = Host.CreateDefaultBuilder(args)
            .UseSerilog((ctx, lc) => lc.WriteTo.Console())
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                if (infisicalSecrets != null)
                {
                   
                    config.AddInMemoryCollection(infisicalSecrets);
                }
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.Configure<RabbitMqOptions>(hostContext.Configuration.GetSection("RabbitMq"));
                services.Configure<SmtpOptions>(hostContext.Configuration.GetSection("Smtp"));

                services.AddSingleton<RabbitMqPersistentConnection>();
                services.AddHostedService(sp => sp.GetRequiredService<RabbitMqPersistentConnection>());

                services.AddScoped<IEmailSender, EmailSender>();
                services.AddHostedService<EmailConsumerService>();
            });

        var host = builder.Build();
        await host.RunAsync();
    }
    
    private static async Task<Secret> GetInfisicalSecret(string secretName)
    {
        var settings = new InfisicalSdkSettingsBuilder().Build();
        var infisicalClient = new InfisicalClient(settings);

        var _ = infisicalClient.Auth().UniversalAuth().LoginAsync(SecretManager.ClientId,
            SecretManager.ClientSecret).Result;

        var getSecretOptions = new GetSecretOptions
        {
            SecretName = secretName,
            EnvironmentSlug = SecretManager.Environment,
            SecretPath = SecretManager.SecretPath,
            ProjectId = SecretManager.ProjectId
        };

        var secret = await infisicalClient.Secrets().GetAsync(getSecretOptions);

        return secret;
    }
   
    private static async Task<Dictionary<string, string>?> FetchSecretsFromInfisical()
    {
        try
        {
            var secrets = new Dictionary<string, string>
            {
                { "Smtp:Host", (await GetInfisicalSecret("SmtpHost")).SecretValue },
                { "Smtp:User", (await GetInfisicalSecret("SmtpUser")).SecretValue },
                { "Smtp:Password", (await GetInfisicalSecret("SmtpPassword")).SecretValue },
                { "Smtp:FromEmail", (await GetInfisicalSecret("SmtpFromEmail")).SecretValue },
                { "Smtp:FromName", (await GetInfisicalSecret("SmtpFromName")).SecretValue },
                {
                    "Smtp:Port", (await GetInfisicalSecret("SmtpPort")).SecretValue
                }
            };

            return secrets;
        }
        catch (Exception)
        {
            return null;
        }
    }
}