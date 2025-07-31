using System.Text;
using Infisical.Sdk;
using Infisical.Sdk.Model;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using SubEmailSender.Config;

namespace SubEmailSender;

class Program
{
    private const string QueueName = "sub-email-sender";
    private const string ExchangeName = "sub-email-sender-exchange";
    private const string RoutingKeyName = "sub-email";

    private static readonly ILogger Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

    private static readonly SecretManager SecretManager = new(
        Environment.GetEnvironmentVariable("INFISICAL_CLIENT_ID"),
        Environment.GetEnvironmentVariable("INFISICAL_CLIENT_SECRET"),
        Environment.GetEnvironmentVariable("INFISICAL_PROJECT_ID")
    );

    private static readonly SmtpServer SmtpServer = new(
        GetInfisicalSecret("SmtpHost").Result.SecretValue,
        GetInfisicalSecret("SmtpUser").Result.SecretValue,
        GetInfisicalSecret("SmtpPassword").Result.SecretValue,
        GetInfisicalSecret("SmtpFromEmail").Result.SecretValue,
        GetInfisicalSecret("SmtpFromName").Result.SecretValue,
        Convert.ToInt32(GetInfisicalSecret("SmtpPort").Result.SecretValue)
    );

    public static async Task Main(string[] args)
    {
        Logger.Information("EmailSubSender Iniciado");

        var factory = new ConnectionFactory();

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Direct);
        await channel.QueueDeclareAsync(queue: QueueName, false, false, false);
        await channel.QueueBindAsync(QueueName, ExchangeName, RoutingKeyName);

        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += OnMessageReceived;

        await channel.BasicConsumeAsync(queue: QueueName, autoAck: true, consumer: consumer);

        Logger.Information("Aguardando mensagens.");
        await Task.Delay(-1);
    }

    private static async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var messageJson = Encoding.UTF8.GetString(ea.Body.ToArray());
            var emailData = JsonConvert.DeserializeObject<EmailToBeSend>(messageJson);

            Logger.Information("Mensagem recebida para: {To}", emailData.To);

            await SendEmailAsync(emailData);
        }
        catch (Exception ex)
        {
            Logger.Error("Erro ao processar mensagem: {Exception}", ex);
        }
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


    private static async Task<string> SendEmailAsync(EmailToBeSend email)
    {
        var message = CreateMimeMessage(email);

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(SmtpServer.Host, SmtpServer.Port, useSsl: false);
            Logger.Information("Conectado ao host SMTP.");

            await client.AuthenticateAsync(SmtpServer.User, SmtpServer.Password);
            Logger.Information("Autenticado com sucesso.");

            await client.SendAsync(message);
            Logger.Information("E-mail enviado para {To}.", email.To);

            await client.DisconnectAsync(true);

            return "success";
        }
        catch (Exception ex)
        {
            Logger.Error("Erro ao enviar e-mail: {Exception}", ex);
            throw;
        }
    }

    private static MimeMessage CreateMimeMessage(EmailToBeSend email)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(SmtpServer.FromName, SmtpServer.FromEmail));
            message.To.Add(new MailboxAddress(email.To, email.To));
            message.Subject = email.Subject;

            message.Body = new BodyBuilder
            {
                HtmlBody = email.Body
            }.ToMessageBody();

            Logger.Information("Email message built");

            return message;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}