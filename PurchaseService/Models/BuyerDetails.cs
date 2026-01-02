namespace PurchaseService.Models;

public class BuyerDetails
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public BuyerAddress Address { get; set; } = new BuyerAddress();
    public string PaymentMethod { get; set; } = string.Empty;
}

public class BuyerAddress
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}