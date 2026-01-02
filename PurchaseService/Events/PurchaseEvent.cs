using PurchaseService.Models;

namespace PurchaseService.Events;

// MassTransit message contracts
public interface IPurchaseCreated
{
    Guid EventId { get; }
    DateTime EventTimestamp { get; }
    int PurchaseId { get; }
    int BuyerId { get; }
    int OfferId { get; }
    DateTime PurchaseDate { get; }
    decimal Amount { get; }
    string Status { get; }
    BuyerDetails BuyerDetails { get; }
    DateTime CreatedAt { get; }
}

public interface IPurchaseUpdated
{
    Guid EventId { get; }
    DateTime EventTimestamp { get; }
    int PurchaseId { get; }
    int BuyerId { get; }
    int OfferId { get; }
    DateTime PurchaseDate { get; }
    decimal Amount { get; }
    string Status { get; }
    BuyerDetails BuyerDetails { get; }
    DateTime UpdatedAt { get; }
}

public class PurchaseCreatedEvent : IPurchaseCreated
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
    public int PurchaseId { get; set; }
    public int BuyerId { get; set; }
    public int OfferId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public BuyerDetails BuyerDetails { get; set; } = new BuyerDetails();
    public DateTime CreatedAt { get; set; }
}

public class PurchaseUpdatedEvent : IPurchaseUpdated
{
    public Guid EventId { get; set; } = Guid.NewGuid();
    public DateTime EventTimestamp { get; set; } = DateTime.UtcNow;
    public int PurchaseId { get; set; }
    public int BuyerId { get; set; }
    public int OfferId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public BuyerDetails BuyerDetails { get; set; } = new BuyerDetails();
    public DateTime UpdatedAt { get; set; }
}