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
        try
        {
            purchase.CreatedAt = DateTime.UtcNow;
            purchase.UpdatedAt = DateTime.UtcNow;

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            // Publish PurchaseCreated event
            var eventData = new PurchaseEventData
            {
                PurchaseId = purchase.PurchaseId,
                BuyerId = purchase.BuyerId,
                OfferId = purchase.OfferId,
                PurchaseDate = purchase.PurchaseDate,
                Amount = purchase.Amount,
                Status = purchase.Status,
                BuyerDetails = purchase.BuyerDetails,
                CreatedAt = purchase.CreatedAt,
                UpdatedAt = purchase.UpdatedAt
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
        //if (id != purchase.PurchaseId)
        //{
        //    return BadRequest("Purchase ID mismatch");
        //}

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

            // Publish PurchaseUpdated event
            var eventData = new PurchaseEventData
            {
                PurchaseId = existingPurchase.PurchaseId,
                BuyerId = existingPurchase.BuyerId,
                OfferId = existingPurchase.OfferId,
                PurchaseDate = existingPurchase.PurchaseDate,
                Amount = existingPurchase.Amount,
                Status = existingPurchase.Status,
                BuyerDetails = existingPurchase.BuyerDetails,
                CreatedAt = existingPurchase.CreatedAt,
                UpdatedAt = existingPurchase.UpdatedAt
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
}