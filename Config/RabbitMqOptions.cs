namespace SubEmailSender.Config;

public class RabbitMqOptions
{
    public string HostName { get; set; } = "rabbitmq";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string QueueName { get; set; } = "sub-email-sender";
    public string ExchangeName { get; set; } = "sub-email-sender-exchange";
    public string RoutingKeyName { get; set; } = "sub-email";
    public ushort PrefetchCount { get; set; } = 10;
}