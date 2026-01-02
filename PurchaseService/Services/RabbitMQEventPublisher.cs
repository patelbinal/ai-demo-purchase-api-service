using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PurchaseService.Configuration;
using PurchaseService.Events;
using PurchaseService.Models;
using PurchaseService.Services;
using RabbitMQ.Client;

namespace PurchaseService.Services;

public class RabbitMQEventPublisher : IEventPublisher, IDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQEventPublisher> _logger;
    private IConnection? _connection;
    private IModel? _channel;
    private bool _disposed = false;

    public RabbitMQEventPublisher(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQEventPublisher> logger)
    {
        _settings = settings.Value;
        _logger = logger;
        InitializeRabbitMQ();
    }

    private void InitializeRabbitMQ()
    {
        try
        {
            var factory = new ConnectionFactory()
            {
                HostName = _settings.HostName,
                Port = _settings.Port,
                UserName = _settings.UserName,
                Password = _settings.Password,
                VirtualHost = _settings.VirtualHost
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare exchange
            _channel.ExchangeDeclare(
                exchange: _settings.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Declare queue
            _channel.QueueDeclare(
                queue: _settings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            // Bind queue to exchange
            _channel.QueueBind(
                queue: _settings.QueueName,
                exchange: _settings.ExchangeName,
                routingKey: "purchase.*");

            _logger.LogInformation("RabbitMQ connection initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ connection");
        }
    }

    public async Task PublishAsync<T>(T eventData, string eventType) where T : class
    {
        if (_channel == null || _connection == null)
        {
            _logger.LogWarning("RabbitMQ connection not available, skipping event publishing");
            return;
        }

        try
        {
            // Extract the appropriate ID to use as entityId
            string entityId = eventData switch
            {
                Purchase purchase => purchase.PurchaseId.ToString(),
                PurchaseEventData purchaseEventData => purchaseEventData.PurchaseId.ToString(),
                _ => Guid.NewGuid().ToString()
            };

            var purchaseEvent = new PurchaseEvent
            {
                EventType = eventType,
                EntityType = "PURCHASE",
                EntityId = entityId,
                OccurredAt = DateTime.UtcNow,
                Payload = eventData // Use the actual data being created/updated
            };

            var message = JsonSerializer.Serialize(purchaseEvent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var body = Encoding.UTF8.GetBytes(message);

            var routingKey = eventType switch
            { 
                "PurchaseCreated" => "search.purchase.created",
                "PurchaseUpdated" => "search.purchase.updated",
                _ => "search.event.unknown"
            };

            _channel.BasicPublish(
                exchange: _settings.ExchangeName,
                routingKey: routingKey,
                basicProperties: null,
                body: body);

            _logger.LogInformation("Published event {EventType} for entity ID {EntityId}", eventType, purchaseEvent.EntityId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", eventType);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection");
        }

        _disposed = true;
    }
}