using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SubEmailSender.Config;

namespace SubEmailSender.Infrastructure;

public class RabbitMqPersistentConnection : IHostedService, IDisposable
{
    public RabbitMqPersistentConnection(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
        
        _factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
        };
    }

    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private bool _disposed;
    private readonly RabbitMqOptions _options;
    
    public async Task<IChannel> CreateChannelAsync()
    {
        if (!_connection.IsOpen)
            throw new InvalidOperationException("RabbitMQ connection is not open");

        return await _connection.CreateChannelAsync();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _connection = await _factory.CreateConnectionAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is { IsOpen: true })
            _connection.CloseAsync();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _connection?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}