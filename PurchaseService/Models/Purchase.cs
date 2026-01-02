using System.ComponentModel.DataAnnotations;

namespace PurchaseService.Models;

public class Purchase
{
    public int PurchaseId { get; set; }
    
    [Required]
    public int BuyerId { get; set; }
    
    [Required]
    public int OfferId { get; set; }
    
    [Required]
    public DateTime PurchaseDate { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }
    
    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Pending";
    
    [Required]
    public BuyerDetails BuyerDetails { get; set; } = new BuyerDetails();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}