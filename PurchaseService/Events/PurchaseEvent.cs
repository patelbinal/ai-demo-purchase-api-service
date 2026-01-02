using PurchaseService.Models;

namespace PurchaseService.Events;

// Generic event envelope interface
public interface IEventEnvelope
{
    string EventType { get; }
    string EntityType { get; }
    string EntityId { get; }
    DateTime OccurredAt { get; }
    object Payload { get; }
}

// Generic event envelope implementation
public class EventEnvelope<T> : IEventEnvelope where T : class
{
    public string EventType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public T Payload { get; set; } = default!;
    
    // For interface compatibility
    object IEventEnvelope.Payload => Payload;
}

// Purchase-specific event envelopes
public interface IPurchaseCreated : IEventEnvelope { }
public interface IPurchaseUpdated : IEventEnvelope { }

public class PurchaseCreated : EventEnvelope<PurchaseCreatedPayload>, IPurchaseCreated
{
    public PurchaseCreated()
    {
        EventType = "PurchaseCreated";
        EntityType = "PURCHASE";
    }
}

public class PurchaseUpdated : EventEnvelope<PurchaseUpdatedPayload>, IPurchaseUpdated
{
    public PurchaseUpdated()
    {
        EventType = "PurchaseUpdated";
        EntityType = "PURCHASE";
    }
}

// Payload classes
public class PurchaseCreatedPayload
{
    public int PurchaseId { get; set; }
    public int BuyerId { get; set; }
    public int OfferId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public BuyerDetails BuyerDetails { get; set; } = new BuyerDetails();
    public DateTime CreatedAt { get; set; }
}

public class PurchaseUpdatedPayload
{
    public int PurchaseId { get; set; }
    public int BuyerId { get; set; }
    public int OfferId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public BuyerDetails BuyerDetails { get; set; } = new BuyerDetails();
    public DateTime UpdatedAt { get; set; }
}