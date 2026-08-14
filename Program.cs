using MassTransit;
using Microsoft.Extensions.Options;
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
					x.SetKebabCaseEndpointNameFormatter();
					x.AddConsumer<EmailConsumer>();

					x.UsingRabbitMq((context, cfg) =>
					{
						var options =
							context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

						cfg.Host(
							options.HostName,
							(ushort)options.Port,
							options.VirtualHost,
							host =>
							{
								host.Username(options.UserName);
								host.Password(options.Password);

								host.RequestedConnectionTimeout(
									TimeSpan.FromSeconds(10));
							});

						cfg.ReceiveEndpoint(
							options.QueueName,
							endpoint =>
							{
								endpoint.Durable = true;
								endpoint.AutoDelete = false;

								endpoint.PrefetchCount =
									options.PrefetchCount;

								endpoint.UseRawJsonDeserializer();

								endpoint.UseMessageRetry(retry =>
								{
									retry.Exponential(
										retryLimit: 3,
										minInterval: TimeSpan.FromSeconds(2),
										maxInterval: TimeSpan.FromSeconds(30),
										intervalDelta: TimeSpan.FromSeconds(5));
								});

								endpoint.ConfigureConsumer<EmailConsumer>(context);
							});
					});
				});
			});

		await builder
			.Build()
			.RunAsync();
	}
}
