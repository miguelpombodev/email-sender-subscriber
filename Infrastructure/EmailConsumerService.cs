using System.Text;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SubEmailSender.Config;
using SubEmailSender.Models;

namespace SubEmailSender.Infrastructure;

public class EmailConsumerService : BackgroundService
{
    public EmailConsumerService(RabbitMqPersistentConnection connection, ILogger<EmailConsumerService> logger,
        IEmailSender emailSender, IOptions<RabbitMqOptions> mqOptions)
    {
        _connection = connection;
        _logger = logger;
        _emailSender = emailSender;
        _mqOptions = mqOptions.Value;
    }

    private readonly RabbitMqPersistentConnection _connection;
    private readonly ILogger<EmailConsumerService> _logger;
    private readonly IEmailSender _emailSender;
    private readonly RabbitMqOptions _mqOptions;
    private IChannel _channel;


    protected override async Task<Task> ExecuteAsync(CancellationToken cancellationToken)
    {
        _channel = await _connection.CreateChannelAsync();

        var queueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", $"{_mqOptions.ExchangeName}.dlx" }
        };

        await _channel.ExchangeDeclareAsync(_mqOptions.ExchangeName, ExchangeType.Direct,
            cancellationToken: cancellationToken);
        await _channel.QueueDeclareAsync(queue: _mqOptions.QueueName, durable: true, exclusive: false,
            autoDelete: false, arguments: queueArgs,
            cancellationToken: cancellationToken);
        await _channel.QueueBindAsync(_mqOptions.QueueName, _mqOptions.ExchangeName, _mqOptions.RoutingKeyName,
            cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(0, _mqOptions.PrefetchCount, false, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceived;

        await _channel.BasicConsumeAsync(queue: _mqOptions.QueueName, autoAck: false, consumer: consumer,
            cancellationToken: cancellationToken);

        _logger.LogInformation("EmailConsumerService started and listening.");

        return Task.CompletedTask;
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
    {
        var deliveryTag = ea.DeliveryTag;
        try
        {
            var messageJson = Encoding.UTF8.GetString(ea.Body.ToArray());
            var emailData = JsonConvert.DeserializeObject<EmailToBeSend>(messageJson);

            if (emailData == null)
                throw new InvalidOperationException("Invalid payload!");

            await _emailSender.SendEmailAsync(emailData);
            
            await _channel.BasicAckAsync(deliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError("Erro ao processar mensagem: {Exception}", ex);
            await _channel!.BasicNackAsync(deliveryTag, false, requeue: false);
        }
    }

    public override async Task<Task> StopAsync(CancellationToken cancellationToken)
    {
        _channel?.CloseAsync();
        return base.StopAsync(cancellationToken);
    }
}