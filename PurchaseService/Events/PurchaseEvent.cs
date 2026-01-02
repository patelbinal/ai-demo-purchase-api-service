using PurchaseService.Models;

namespace PurchaseService.Events;

public class PurchaseEvent
{
    public string EventType { get; set; } = string.Empty; // "PurchaseCreated", etc.
    public string EntityType { get; set; } = "PURCHASE";
    public string EntityId { get; set; } = Guid.NewGuid().ToString();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public object Payload { get; set; } = new object();
}

public class PurchaseEventData
{
    // Identity
    public string PurchaseId { get; set; } = default!;
    public string BuyerId { get; set; } = default!;
    public string OfferId { get; set; } = default!;
    // Purchase details
    public decimal Amount { get; set; }
    public string Status { get; set; } = default!;
    public DateTime PurchaseDate { get; set; }
}