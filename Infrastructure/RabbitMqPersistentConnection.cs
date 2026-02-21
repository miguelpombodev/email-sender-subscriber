using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SubEmailSender.Config;

namespace SubEmailSender.Infrastructure;

public class RabbitMqPersistentConnection : IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly RabbitMqOptions _options;
    private IConnection? _connection;
    private bool _disposed;
    private readonly ILogger<RabbitMqPersistentConnection> _logger;
    private readonly int _maxRetries = 10;

    public RabbitMqPersistentConnection(IOptions<RabbitMqOptions> options, ILogger<RabbitMqPersistentConnection> logger)
    {
        _logger = logger;
        _options = options.Value;

        _factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
        };
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public async Task EnsureConnectedAsync(CancellationToken cancellationToken)
    {
        if (IsConnected)
            return;

        var delay = TimeSpan.FromSeconds(5);

        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                _connection = await _factory.CreateConnectionAsync();

                if (_connection.IsOpen)
                {
                    RegisterEventHandlers();
                    _logger.LogInformation("RabbitMQ connection established");
                    return;
                }
            }
            catch (Exception e)
            {
                if (attempt == _maxRetries)
                    throw;
                
                _logger.LogError(e, $"RabbitMQ not ready. Attempt {attempt}/{_maxRetries}");
                await Task.Delay(delay, cancellationToken);
            }
        }
    }

    public async Task<IChannel> CreateChannelAsync()
    {
        if (!IsConnected)
            throw new InvalidOperationException("No RabbitMQ connection available");

        return await _connection!.CreateChannelAsync();
    }

    private void RegisterEventHandlers()
    {
        _connection!.ConnectionShutdownAsync += async (_, _) =>
        {
            _logger.LogInformation("RabbitMQ connection shutdown. Reconnecting...");
            await EnsureConnectedAsync(CancellationToken.None);


        };

        _connection!.CallbackExceptionAsync += async (_, _) =>
        {
            _logger.LogInformation("RabbitMQ callback exception. Reconnecting...");
            await EnsureConnectedAsync(CancellationToken.None);
        };
    }
    

    public void Dispose()
    {
        if (_disposed) return;

        _connection?.Dispose();
        _disposed = true;
    }
}