using MassTransit;
using Serilog;
using SubEmailSender.Config;
using SubEmailSender.Infrastructure;

namespace SubEmailSender;

public static class Program
{
	public async static Task Main(string[] args)
	{
		IHostBuilder builder = Host.CreateDefaultBuilder(args)
			.UseSerilog((ctx, logger) =>
			{
				logger
					.ReadFrom.Configuration(ctx.Configuration)
					.Enrich.FromLogContext()
					.WriteTo.Console();
			})
			.ConfigureServices((hostContext, services) =>
			{
				IConfiguration configuration = hostContext.Configuration;

				services
					.AddOptions<SmtpOptions>()
					.Bind(configuration.GetSection("Smtp"))
					.ValidateDataAnnotations()
					.ValidateOnStart();

				services
					.AddOptions<RabbitMqOptions>()
					.Bind(configuration.GetSection("RabbitMq"))
					.ValidateOnStart();

				services.AddSingleton<IEmailSender, EmailSender>();

				services.AddMassTransit(x =>
				{
					x.AddConsumer<EmailConsumer>();

					x.UsingRabbitMq((context, cfg) =>
					{
						RabbitMqOptions rabbitMqOptions =
							context.GetRequiredService<
									Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>()
								.Value;

						cfg.Host(
							rabbitMqOptions.HostName,
							(ushort)rabbitMqOptions.Port,
							rabbitMqOptions.VirtualHost,
							host =>
							{
								host.Username(rabbitMqOptions.UserName);
								host.Password(rabbitMqOptions.Password);
							});

						cfg.ReceiveEndpoint(
							rabbitMqOptions.QueueName,
							endpoint =>
							{
								endpoint.PrefetchCount =
									rabbitMqOptions.PrefetchCount;
								
								endpoint.UseRawJsonDeserializer();
								
								endpoint.UseMessageRetry(retry =>
								{
									retry.Immediate(2);
								});

								endpoint.ConfigureConsumer<EmailConsumer>(
									context);
							});
					});
				});
			});

		await builder
			.Build()
			.RunAsync();
	}
}
