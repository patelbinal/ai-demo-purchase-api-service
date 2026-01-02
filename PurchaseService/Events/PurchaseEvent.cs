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
    public int PurchaseId { get; set; }
    public int BuyerId { get; set; }
    public int OfferId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public BuyerDetails BuyerDetails { get; set; } = new BuyerDetails();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}