using PurchaseService.Events;

namespace PurchaseService.Services;

public interface IEventPublisher
{
    Task PublishAsync<T>(T eventData, string eventType) where T : class;
}