namespace PurchaseService.Services;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message) where T : class;
}