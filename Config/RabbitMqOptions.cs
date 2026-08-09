namespace SubEmailSender.Config;

public class RabbitMqOptions
{
	public string HostName { get; set; } = "rabbitmq";
	public int Port { get; set; } = 5672;
	
	public string VirtualHost { get; set; } = "/";
	
	public string UserName { get; set; } = "admin";
	public string Password { get; set; } = "admin123";

	public string QueueName { get; set; } = "sub-email-sender";

	public ushort PrefetchCount { get; set; } = 10;
}
