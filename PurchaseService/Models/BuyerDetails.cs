using System.ComponentModel.DataAnnotations;

namespace PurchaseService.Models;

public class BuyerDetails
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;
    
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [Required]
    public BuyerAddress Address { get; set; } = new BuyerAddress();
    
    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;
}

public class BuyerAddress
{
    [Required]
    [StringLength(200)]
    public string Street { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string State { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string ZipCode { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string Country { get; set; } = string.Empty;
}