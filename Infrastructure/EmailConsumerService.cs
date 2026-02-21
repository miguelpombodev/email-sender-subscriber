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
    private readonly RabbitMqPersistentConnection _connection;
    private readonly ILogger<EmailConsumerService> _logger;
    private readonly IEmailSender _emailSender;
    private readonly RabbitMqOptions _mqOptions;

    private IChannel? _channel;

    public EmailConsumerService(
        RabbitMqPersistentConnection connection,
        ILogger<EmailConsumerService> logger,
        IEmailSender emailSender,
        IOptions<RabbitMqOptions> mqOptions)
    {
        _connection = connection;
        _logger = logger;
        _emailSender = emailSender;
        _mqOptions = mqOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await StartConsumerAsync(stoppingToken);

        _logger.LogInformation("EmailConsumerService started and listening.");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task StartConsumerAsync(CancellationToken cancellationToken)
    {
        await _connection.EnsureConnectedAsync(cancellationToken);

        _channel = await _connection.CreateChannelAsync();

        RegisterChannelEvents(_channel);

        var queueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", $"{_mqOptions.ExchangeName}.dlx" }
        };

        await _channel.ExchangeDeclareAsync(
            _mqOptions.ExchangeName,
            ExchangeType.Direct,
            durable: true,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: _mqOptions.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArgs,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            _mqOptions.QueueName,
            _mqOptions.ExchangeName,
            _mqOptions.RoutingKeyName,
            cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(
            0,
            _mqOptions.PrefetchCount,
            false,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceived;

        await _channel.BasicConsumeAsync(
            queue: _mqOptions.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);
    }

    private async Task OnMessageReceived(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel == null)
        {
            _logger.LogWarning("Channel not available when receiving message.");
            return;
        }

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
            _logger.LogError(ex, "Erro ao processar mensagem");

            await _channel.BasicNackAsync(deliveryTag, multiple: false, requeue: false);
        }
    }

    private void RegisterChannelEvents(IChannel channel)
    {
        channel.ChannelShutdownAsync += async (_, _) =>
        {
            _logger.LogWarning("Channel shutdown detected. Restarting consumer...");

            try
            {
                await StartConsumerAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart consumer after channel shutdown.");
            }
        };
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null && _channel.IsOpen)
        {
            await _channel.CloseAsync(cancellationToken);
            _channel.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }
}