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
        var message = context.Message;
        
        _logger.LogInformation(
            "Received PurchaseCreated event - PurchaseId: {PurchaseId}, BuyerId: {BuyerId}, Amount: {Amount}",
            message.PurchaseId, message.BuyerId, message.Amount);

        // Add your business logic here
        // For example:
        // - Send notification emails
        // - Update analytics
        // - Trigger other business processes
        
        await Task.CompletedTask;
    }
}