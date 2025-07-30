using System.Text;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

namespace SubEmailSender;

class Program
{
    public static IConfigurationRoot Configuration { get; } = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

    private const string QueueName = "sub-email-sender";
    private const string ExchangeName = "sub-email-sender-exchange";
    private const string RoutingKeyName = "sub-email";
    
    private static readonly ILogger Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger(); 

    private static readonly IConfigurationSection Settings = Configuration.GetRequiredSection("Settings");

    private static readonly string SmtpHost = Settings["Host"] ?? throw new ArgumentNullException("Host not configured!");
    private static readonly int SmtpPort = Convert.ToInt32(Settings["Port"]);
    private static readonly string SmtpUser = Settings["User"] ?? throw new ArgumentNullException("User not configured!");
    private static readonly string SmtpPass = Settings["Password"] ?? throw new ArgumentNullException("Password not configured!");
    private static readonly string FromEmail = Settings["FromEmail"] ?? throw new ArgumentNullException("FromEmail not configured!");
    private static readonly string FromName = Settings["FromName"] ?? throw new ArgumentNullException("FromName not configured!");
    
    
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


    private static async Task<string> SendEmailAsync(EmailToBeSend email)
    {
        var message = CreateMimeMessage(email);

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(SmtpHost, SmtpPort, useSsl: false);
            Logger.Information("Conectado ao host SMTP.");

            await client.AuthenticateAsync(SmtpUser, SmtpPass);
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
            message.From.Add(new MailboxAddress(FromName, FromEmail));
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