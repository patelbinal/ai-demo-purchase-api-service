using PurchaseService.Models;

namespace PurchaseService.Events;

public class PurchaseEvent
{
    public string EventType { get; set; } = string.Empty; // "PurchaseCreated" or "PurchaseUpdated"
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string EntityType = "Purchase";
    public PurchaseEventData Payload { get; set; } = new PurchaseEventData();
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