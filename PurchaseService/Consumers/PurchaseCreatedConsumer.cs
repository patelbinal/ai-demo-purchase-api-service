using MassTransit;
using PurchaseService.Events;

namespace PurchaseService.Consumers;

public class PurchaseCreatedConsumer : IConsumer<IPurchaseCreated>
{
    private readonly ILogger<PurchaseCreatedConsumer> _logger;

    public PurchaseCreatedConsumer(ILogger<PurchaseCreatedConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<IPurchaseCreated> context)
    {
        var envelope = context.Message;
        var payload = (envelope as PurchaseCreated)?.Payload;
        
        if (payload != null)
        {
            _logger.LogInformation(
                "Received {EventType} event - EntityType: {EntityType}, EntityId: {EntityId}, PurchaseId: {PurchaseId}, BuyerId: {BuyerId}, Amount: {Amount}",
                envelope.EventType, envelope.EntityType, envelope.EntityId, payload.PurchaseId, payload.BuyerId, payload.Amount);

            // Add your business logic here
            // For example:
            // - Send notification emails
            // - Update analytics
            // - Trigger other business processes
        }
        
        await Task.CompletedTask;
    }
}