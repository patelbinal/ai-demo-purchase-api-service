using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PurchaseService.Data;
using PurchaseService.Events;
using PurchaseService.Models;
using PurchaseService.Services;

namespace PurchaseService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly PurchaseDbContext _context;
    private readonly ILogger<PurchasesController> _logger;
    private readonly IEventPublisher _eventPublisher;

    public PurchasesController(
        PurchaseDbContext context, 
        ILogger<PurchasesController> logger,
        IEventPublisher eventPublisher)
    {
        _context = context;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    // GET: api/purchases
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchases(
        [FromQuery] int? buyerId = null,
        [FromQuery] int? offerId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        // Validate query parameters
        if (page < 1)
            return BadRequest("Page number must be greater than 0");
            
        if (pageSize < 1 || pageSize > 100)
            return BadRequest("Page size must be between 1 and 100");
            
        if (buyerId.HasValue && buyerId <= 0)
            return BadRequest("BuyerId must be greater than 0");
            
        if (offerId.HasValue && offerId <= 0)
            return BadRequest("OfferId must be greater than 0");
            
        if (!string.IsNullOrEmpty(status) && !IsValidStatus(status))
            return BadRequest("Invalid status. Valid values are: Pending, Processing, Completed, Cancelled, Refunded");

        try
        {
            var query = _context.Purchases.AsQueryable();

            if (buyerId.HasValue)
                query = query.Where(p => p.BuyerId == buyerId.Value);

            if (offerId.HasValue)
                query = query.Where(p => p.OfferId == offerId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var purchases = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(purchases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchases");
            return StatusCode(500, "An error occurred while retrieving purchases");
        }
    }

    // GET: api/purchases/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<Purchase>> GetPurchase(int id)
    {
        if (id <= 0)
            return BadRequest("Purchase ID must be greater than 0");
            
        try
        {
            var purchase = await _context.Purchases.FindAsync(id);

            if (purchase == null)
            {
                return NotFound($"Purchase with ID {id} not found");
            }

            return purchase;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchase {PurchaseId}", id);
            return StatusCode(500, "An error occurred while retrieving the purchase");
        }
    }

    // POST: api/purchases
    [HttpPost]
    public async Task<ActionResult<Purchase>> CreatePurchase(Purchase purchase)
    {
        // Validate model state
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Additional business logic validation
        var validationResult = ValidatePurchaseForCreation(purchase);
        if (validationResult != null)
            return validationResult;

        try
        {
            purchase.CreatedAt = DateTime.UtcNow;
            purchase.UpdatedAt = DateTime.UtcNow;

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            // Publish PurchaseCreated event with specific event data structure
            var eventData = new PurchaseEventData
            {
                PurchaseId = purchase.PurchaseId.ToString(),
                BuyerId = purchase.BuyerId.ToString(),
                OfferId = purchase.OfferId.ToString(),
                Amount = purchase.Amount,
                Status = purchase.Status,
                PurchaseDate = purchase.PurchaseDate
            };

            await _eventPublisher.PublishAsync(eventData, "PurchaseCreated");

            _logger.LogInformation("Purchase created with ID {PurchaseId} and event published", purchase.PurchaseId);

            return CreatedAtAction(nameof(GetPurchase), new { id = purchase.PurchaseId }, purchase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase");
            return StatusCode(500, "An error occurred while creating the purchase");
        }
    }

    // PUT: api/purchases/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdatePurchase(int id, Purchase purchase)
    {
        if (id <= 0)
            return BadRequest("Purchase ID must be greater than 0");
            
        // Validate model state
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Additional business logic validation
        var validationResult = ValidatePurchaseForUpdate(purchase);
        if (validationResult != null)
            return validationResult;

        try
        {
            var existingPurchase = await _context.Purchases.FindAsync(id);
            if (existingPurchase == null)
            {
                return NotFound($"Purchase with ID {id} not found");
            }

            existingPurchase.BuyerId = purchase.BuyerId;
            existingPurchase.OfferId = purchase.OfferId;
            existingPurchase.PurchaseDate = purchase.PurchaseDate;
            existingPurchase.Amount = purchase.Amount;
            existingPurchase.Status = purchase.Status;
            existingPurchase.BuyerDetails = purchase.BuyerDetails;
            existingPurchase.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload the entity to ensure we have all the updated data
            await _context.Entry(existingPurchase).ReloadAsync();

            // Publish PurchaseUpdated event with specific event data structure
            var eventData = new PurchaseEventData
            {
                PurchaseId = existingPurchase.PurchaseId.ToString(),
                BuyerId = existingPurchase.BuyerId.ToString(),
                OfferId = existingPurchase.OfferId.ToString(),
                Amount = existingPurchase.Amount,
                Status = existingPurchase.Status,
                PurchaseDate = existingPurchase.PurchaseDate
            };

            await _eventPublisher.PublishAsync(eventData, "PurchaseUpdated");

            _logger.LogInformation("Purchase updated with ID {PurchaseId} and event published", existingPurchase.PurchaseId);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase {PurchaseId}", id);
            return StatusCode(500, "An error occurred while updating the purchase");
        }
    }

    // DELETE: api/purchases/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePurchase(int id)
    {
        if (id <= 0)
            return BadRequest("Purchase ID must be greater than 0");
            
        try
        {
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase == null)
            {
                return NotFound($"Purchase with ID {id} not found");
            }

            _context.Purchases.Remove(purchase);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting purchase {PurchaseId}", id);
            return StatusCode(500, "An error occurred while deleting the purchase");
        }
    }

    // GET: api/purchases/buyer/{buyerId}
    [HttpGet("buyer/{buyerId}")]
    public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchasesByBuyer(int buyerId)
    {
        if (buyerId <= 0)
            return BadRequest("Buyer ID must be greater than 0");
            
        try
        {
            var purchases = await _context.Purchases
                .Where(p => p.BuyerId == buyerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(purchases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchases for buyer {BuyerId}", buyerId);
            return StatusCode(500, "An error occurred while retrieving purchases");
        }
    }

    // GET: api/purchases/offer/{offerId}
    [HttpGet("offer/{offerId}")]
    public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchasesByOffer(int offerId)
    {
        if (offerId <= 0)
            return BadRequest("Offer ID must be greater than 0");
            
        try
        {
            var purchases = await _context.Purchases
                .Where(p => p.OfferId == offerId)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(purchases);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving purchases for offer {OfferId}", offerId);
            return StatusCode(500, "An error occurred while retrieving purchases");
        }
    }

    #region Validation Helper Methods
    
    private static bool IsValidStatus(string status)
    {
        var validStatuses = new[] { "Pending", "Processing", "Completed", "Cancelled", "Refunded" };
        return validStatuses.Contains(status, StringComparer.OrdinalIgnoreCase);
    }
    
    private static bool IsValidPaymentMethod(string paymentMethod)
    {
        var validMethods = new[] { "CreditCard", "DebitCard", "PayPal", "BankTransfer", "Cash" };
        return validMethods.Contains(paymentMethod, StringComparer.OrdinalIgnoreCase);
    }
    
    private ActionResult? ValidatePurchaseForCreation(Purchase purchase)
    {
        // Validate purchase date is not in the future beyond a reasonable threshold
        if (purchase.PurchaseDate > DateTime.UtcNow.AddMinutes(5))
            return BadRequest("Purchase date cannot be more than 5 minutes in the future");
        
        // Validate purchase date is not too old
        if (purchase.PurchaseDate < DateTime.UtcNow.AddYears(-5))
            return BadRequest("Purchase date cannot be more than 5 years in the past");
            
        // Validate status
        if (!IsValidStatus(purchase.Status))
            return BadRequest("Invalid status. Valid values are: Pending, Processing, Completed, Cancelled, Refunded");
            
        // Validate payment method
        if (!string.IsNullOrEmpty(purchase.BuyerDetails?.PaymentMethod) && 
            !IsValidPaymentMethod(purchase.BuyerDetails.PaymentMethod))
            return BadRequest("Invalid payment method. Valid values are: CreditCard, DebitCard, PayPal, BankTransfer, Cash");
            
        return null;
    }
    
    private ActionResult? ValidatePurchaseForUpdate(Purchase purchase)
    {
        // Same validation as creation, but may have different rules for updates
        return ValidatePurchaseForCreation(purchase);
    }
    
    #endregion
}