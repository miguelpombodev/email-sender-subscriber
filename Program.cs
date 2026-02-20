using Infisical.Sdk;
using Infisical.Sdk.Model;
using Serilog;
using SubEmailSender.Config;
using SubEmailSender.Infrastructure;

namespace SubEmailSender;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args)
            .UseSerilog((ctx, logger) =>
            {
                logger.ReadFrom.Configuration(ctx.Configuration).Enrich.FromLogContext().WriteTo.Console();
            })
            .ConfigureServices((hostContext, services) =>
            {
                var configuration = hostContext.Configuration;
                services
                    .AddOptions<SmtpOptions>()
                    .Bind(configuration.GetSection("Smtp"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();

                services
                    .AddOptions<RabbitMqOptions>()
                    .Bind(configuration.GetSection("RabbitMq"))
                    .ValidateOnStart();

                services.AddSingleton<RabbitMqPersistentConnection>();
                services.AddHostedService(sp => sp.GetRequiredService<RabbitMqPersistentConnection>());

                services.AddSingleton<IEmailSender, EmailSender>();
                services.AddHostedService<EmailConsumerService>();
            });

        await builder.Build().RunAsync();
    }
}