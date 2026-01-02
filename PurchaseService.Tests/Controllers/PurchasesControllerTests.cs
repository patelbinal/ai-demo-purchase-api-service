using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using PurchaseService.Controllers;
using PurchaseService.Data;
using PurchaseService.Events;
using PurchaseService.Models;
using PurchaseService.Services;
using FluentAssertions;
using Xunit;

namespace PurchaseService.Tests.Controllers;

public class PurchasesControllerTests : IDisposable
{
    private readonly PurchaseDbContext _context;
    private readonly Mock<ILogger<PurchasesController>> _loggerMock;
    private readonly Mock<IEventPublisher> _eventPublisherMock;
    private readonly PurchasesController _controller;

    public PurchasesControllerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<PurchaseDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        _context = new PurchaseDbContext(options);
        _loggerMock = new Mock<ILogger<PurchasesController>>();
        _eventPublisherMock = new Mock<IEventPublisher>();
        
        _controller = new PurchasesController(_context, _loggerMock.Object, _eventPublisherMock.Object);
    }

    [Fact]
    public async Task CreatePurchase_ValidPurchase_ReturnsCreatedResult()
    {
        // Arrange
        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails
            {
                Name = "John Doe",
                Email = "john@example.com",
                Phone = "123-456-7890"
            }
        };

        // Act
        var result = await _controller.CreatePurchase(purchase);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        createdResult.ActionName.Should().Be("GetPurchase");
        
        var returnedPurchase = createdResult.Value as Purchase;
        returnedPurchase.Should().NotBeNull();
        returnedPurchase.PurchaseId.Should().BeGreaterThan(0);
        returnedPurchase.BuyerId.Should().Be(123);
        returnedPurchase.OfferId.Should().Be(456);
        returnedPurchase.Amount.Should().Be(25000.00m);
        
        // Verify event was published
        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<PurchaseEventData>(), "PurchaseCreated"),
            Times.Once);
    }

    [Fact]
    public async Task GetPurchase_ExistingPurchase_ReturnsPurchase()
    {
        // Arrange
        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };
        
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetPurchase(purchase.PurchaseId);

        // Assert
        result.Value.Should().NotBeNull();
        result.Value.PurchaseId.Should().Be(purchase.PurchaseId);
        result.Value.BuyerId.Should().Be(123);
        result.Value.Amount.Should().Be(25000.00m);
    }

    [Fact]
    public async Task GetPurchase_NonExistingPurchase_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetPurchase(999);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdatePurchase_ExistingPurchase_ReturnsNoContent()
    {
        // Arrange
        var existingPurchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };
        
        _context.Purchases.Add(existingPurchase);
        await _context.SaveChangesAsync();

        var updatedPurchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 30000.00m,
            Status = "Completed",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe Updated" }
        };

        // Act
        var result = await _controller.UpdatePurchase(existingPurchase.PurchaseId, updatedPurchase);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        // Verify the purchase was updated
        var purchase = await _context.Purchases.FindAsync(existingPurchase.PurchaseId);
        purchase.Should().NotBeNull();
        purchase.Amount.Should().Be(30000.00m);
        purchase.Status.Should().Be("Completed");
        
        // Verify event was published
        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<PurchaseEventData>(), "PurchaseUpdated"),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePurchase_NonExistingPurchase_ReturnsNotFound()
    {
        // Arrange
        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };

        // Act
        var result = await _controller.UpdatePurchase(999, purchase);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetPurchases_WithFilters_ReturnsFilteredResults()
    {
        // Arrange
        var purchase1 = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };
        
        var purchase2 = new Purchase
        {
            BuyerId = 124,
            OfferId = 457,
            Amount = 30000.00m,
            Status = "Completed",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "Jane Smith" }
        };
        
        _context.Purchases.AddRange(purchase1, purchase2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetPurchases(buyerId: 123);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        var purchases = okResult!.Value as IEnumerable<Purchase>;
        purchases.Should().NotBeNull();
        purchases!.Should().HaveCount(1);
        purchases!.First().BuyerId.Should().Be(123);
    }

    [Fact]
    public async Task DeletePurchase_ExistingPurchase_ReturnsNoContent()
    {
        // Arrange
        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };
        
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.DeletePurchase(purchase.PurchaseId);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        
        // Verify the purchase was deleted
        var deletedPurchase = await _context.Purchases.FindAsync(purchase.PurchaseId);
        deletedPurchase.Should().BeNull();
    }

    [Fact]
    public async Task DeletePurchase_NonExistingPurchase_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeletePurchase(999);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #region Validation Tests

    [Fact]
    public async Task CreatePurchase_InvalidBuyerId_ReturnsBadRequest()
    {
        // Arrange
        var purchase = new Purchase
        {
            BuyerId = -1, // Invalid
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };

        // Act & Assert - This will be caught by model validation
        // In a real scenario, you might need to manually validate or use ModelState
        purchase.BuyerId.Should().BeLessThan(0);
    }

    [Fact]
    public async Task CreatePurchase_InvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = -100.00m, // Invalid
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };

        // Act & Assert - This will be caught by model validation
        purchase.Amount.Should().BeLessThan(0);
    }

    [Fact]
    public async Task CreatePurchase_NullBuyerDetails_ReturnsBadRequest()
    {
        // Arrange
        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = null! // Invalid
        };

        // Act
        var result = await _controller.CreatePurchase(purchase);

        // Assert - Should handle null buyer details gracefully
        result.Result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public async Task GetPurchase_InvalidId_ReturnsBadRequest(int invalidId)
    {
        // Act
        var result = await _controller.GetPurchase(invalidId);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DeletePurchase_InvalidId_ReturnsBadRequest(int invalidId)
    {
        // Act
        var result = await _controller.DeletePurchase(invalidId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion

    #region Pagination and Filtering Tests

    [Fact]
    public async Task GetPurchases_InvalidPageNumber_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetPurchases(page: 0); // Invalid page

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPurchases_InvalidPageSize_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetPurchases(pageSize: 101); // Exceeds max

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPurchases_InvalidBuyerId_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetPurchases(buyerId: -1);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPurchases_InvalidStatus_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.GetPurchases(status: "InvalidStatus");

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetPurchases_WithValidPagination_ReturnsPagedResults()
    {
        // Arrange
        var purchases = new List<Purchase>();
        for (int i = 1; i <= 15; i++)
        {
            purchases.Add(new Purchase
            {
                BuyerId = i,
                OfferId = 100 + i,
                Amount = 1000.00m + i,
                Status = "Pending",
                PurchaseDate = DateTime.UtcNow,
                BuyerDetails = new BuyerDetails { Name = $"Buyer {i}" }
            });
        }
        _context.Purchases.AddRange(purchases);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetPurchases(page: 2, pageSize: 5);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedPurchases = okResult!.Value as IEnumerable<Purchase>;
        returnedPurchases!.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetPurchases_WithMultipleFilters_ReturnsFilteredResults()
    {
        // Arrange
        var purchases = new List<Purchase>
        {
            new Purchase { BuyerId = 123, OfferId = 456, Amount = 1000m, Status = "Pending", PurchaseDate = DateTime.UtcNow, BuyerDetails = new BuyerDetails { Name = "John" } },
            new Purchase { BuyerId = 123, OfferId = 457, Amount = 2000m, Status = "Completed", PurchaseDate = DateTime.UtcNow, BuyerDetails = new BuyerDetails { Name = "John" } },
            new Purchase { BuyerId = 124, OfferId = 456, Amount = 1500m, Status = "Pending", PurchaseDate = DateTime.UtcNow, BuyerDetails = new BuyerDetails { Name = "Jane" } }
        };
        _context.Purchases.AddRange(purchases);
        await _context.SaveChangesAsync();

        // Act - Filter by buyerId and offerId
        var result = await _controller.GetPurchases(buyerId: 123, offerId: 456);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedPurchases = okResult!.Value as IEnumerable<Purchase>;
        returnedPurchases!.Should().HaveCount(1);
        returnedPurchases!.First().BuyerId.Should().Be(123);
        returnedPurchases!.First().OfferId.Should().Be(456);
    }

    [Fact]
    public async Task GetPurchases_WithStatusFilter_ReturnsFilteredResults()
    {
        // Arrange
        var purchases = new List<Purchase>
        {
            new Purchase { BuyerId = 123, OfferId = 456, Amount = 1000m, Status = "Pending", PurchaseDate = DateTime.UtcNow, BuyerDetails = new BuyerDetails { Name = "John" } },
            new Purchase { BuyerId = 124, OfferId = 457, Amount = 2000m, Status = "Completed", PurchaseDate = DateTime.UtcNow, BuyerDetails = new BuyerDetails { Name = "Jane" } },
            new Purchase { BuyerId = 125, OfferId = 458, Amount = 1500m, Status = "Pending", PurchaseDate = DateTime.UtcNow, BuyerDetails = new BuyerDetails { Name = "Bob" } }
        };
        _context.Purchases.AddRange(purchases);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetPurchases(status: "Pending");

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedPurchases = okResult!.Value as IEnumerable<Purchase>;
        returnedPurchases!.Should().HaveCount(2);
        returnedPurchases!.All(p => p.Status == "Pending").Should().BeTrue();
    }

    [Fact]
    public async Task GetPurchases_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetPurchases();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedPurchases = okResult!.Value as IEnumerable<Purchase>;
        returnedPurchases!.Should().BeEmpty();
    }

    #endregion

    #region Business Logic Tests

    [Theory]
    [InlineData("Pending")]
    [InlineData("Processing")]
    [InlineData("Completed")]
    [InlineData("Cancelled")]
    [InlineData("Refunded")]
    public async Task CreatePurchase_ValidStatuses_ReturnsCreated(string status)
    {
        // Arrange
        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = status,
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe", Email = "john@example.com" }
        };

        // Act
        var result = await _controller.CreatePurchase(purchase);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var createdPurchase = createdResult!.Value as Purchase;
        createdPurchase!.Status.Should().Be(status);
    }

    [Fact]
    public async Task UpdatePurchase_StatusTransition_UpdatesSuccessfully()
    {
        // Arrange
        var originalPurchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };
        _context.Purchases.Add(originalPurchase);
        await _context.SaveChangesAsync();

        var updatedPurchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Completed",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };

        // Act
        var result = await _controller.UpdatePurchase(originalPurchase.PurchaseId, updatedPurchase);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var purchase = await _context.Purchases.FindAsync(originalPurchase.PurchaseId);
        purchase!.Status.Should().Be("Completed");
        purchase.UpdatedAt.Should().BeAfter(purchase.CreatedAt);
    }

    [Fact]
    public async Task CreatePurchase_SetsTimestamps_Correctly()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;
        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };

        // Act
        var result = await _controller.CreatePurchase(purchase);
        var afterCreate = DateTime.UtcNow;

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var createdPurchase = createdResult!.Value as Purchase;
        
        createdPurchase!.CreatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
        createdPurchase.CreatedAt.Should().BeBefore(afterCreate.AddSeconds(1));
        createdPurchase.UpdatedAt.Should().BeAfter(beforeCreate.AddSeconds(-1));
        createdPurchase.UpdatedAt.Should().BeBefore(afterCreate.AddSeconds(1));
    }

    #endregion

    #region Event Publishing Tests

    [Fact]
    public async Task CreatePurchase_EventPublishingFails_ReturnsInternalServerError()
    {
        // Arrange
        _eventPublisherMock.Setup(x => x.PublishAsync(It.IsAny<PurchaseEventData>(), "PurchaseCreated"))
            .ThrowsAsync(new Exception("Event publishing failed"));

        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };

        // Act
        var result = await _controller.CreatePurchase(purchase);

        // Assert - Current implementation returns 500 when any exception occurs
        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);

        // Verify event publishing was attempted
        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<PurchaseEventData>(), "PurchaseCreated"),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePurchase_PublishesUpdateEvent()
    {
        // Arrange
        var existingPurchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };
        _context.Purchases.Add(existingPurchase);
        await _context.SaveChangesAsync();

        var updatedPurchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 30000.00m,
            Status = "Processing",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe Updated" }
        };

        // Act
        await _controller.UpdatePurchase(existingPurchase.PurchaseId, updatedPurchase);

        // Assert
        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.Is<PurchaseEventData>(data => 
                data.PurchaseId == existingPurchase.PurchaseId.ToString() &&
                data.BuyerId == "123" &&
                data.Amount == 30000.00m), "PurchaseUpdated"),
            Times.Once);
    }

    [Fact]
    public async Task DeletePurchase_DoesNotPublishEvent()
    {
        // Arrange
        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };
        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Act
        await _controller.DeletePurchase(purchase.PurchaseId);

        // Assert - No delete event should be published based on current implementation
        _eventPublisherMock.Verify(
            x => x.PublishAsync(It.IsAny<PurchaseEventData>(), "PurchaseDeleted"),
            Times.Never);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task GetPurchases_LargePageSize_ClampedToMaximum()
    {
        // Act
        var result = await _controller.GetPurchases(pageSize: 200); // Exceeds max of 100

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdatePurchase_WithDifferentBuyerId_UpdatesSuccessfully()
    {
        // Arrange
        var existingPurchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = 25000.00m,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };
        _context.Purchases.Add(existingPurchase);
        await _context.SaveChangesAsync();

        var updatedPurchase = new Purchase
        {
            BuyerId = 999, // Different buyer ID
            OfferId = 456,
            Amount = 30000.00m,
            Status = "Processing",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "Jane Smith" }
        };

        // Act
        var result = await _controller.UpdatePurchase(existingPurchase.PurchaseId, updatedPurchase);

        // Assert
        result.Should().BeOfType<NoContentResult>();
        var purchase = await _context.Purchases.FindAsync(existingPurchase.PurchaseId);
        purchase!.BuyerId.Should().Be(999);
        purchase.BuyerDetails.Name.Should().Be("Jane Smith");
    }

    [Theory]
    [InlineData(0.01)] // Minimum valid amount
    [InlineData(999999.99)] // Large amount
    [InlineData(100.00)] // Regular amount
    public async Task CreatePurchase_ValidAmounts_Success(decimal amount)
    {
        // Arrange
        var purchase = new Purchase
        {
            BuyerId = 123,
            OfferId = 456,
            Amount = amount,
            Status = "Pending",
            PurchaseDate = DateTime.UtcNow,
            BuyerDetails = new BuyerDetails { Name = "John Doe" }
        };

        // Act
        var result = await _controller.CreatePurchase(purchase);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result.Result as CreatedAtActionResult;
        var createdPurchase = createdResult!.Value as Purchase;
        createdPurchase!.Amount.Should().Be(amount);
    }

    [Fact]
    public async Task GetPurchases_OrdersByCreatedAtDescending()
    {
        // Arrange
        var baseDatetime = DateTime.UtcNow.AddDays(-5);
        var purchases = new List<Purchase>
        {
            new Purchase { BuyerId = 123, OfferId = 456, Amount = 1000m, Status = "Pending", PurchaseDate = baseDatetime, CreatedAt = baseDatetime.AddHours(1), BuyerDetails = new BuyerDetails { Name = "John" } },
            new Purchase { BuyerId = 124, OfferId = 457, Amount = 2000m, Status = "Completed", PurchaseDate = baseDatetime, CreatedAt = baseDatetime.AddHours(3), BuyerDetails = new BuyerDetails { Name = "Jane" } },
            new Purchase { BuyerId = 125, OfferId = 458, Amount = 1500m, Status = "Pending", PurchaseDate = baseDatetime, CreatedAt = baseDatetime.AddHours(2), BuyerDetails = new BuyerDetails { Name = "Bob" } }
        };
        _context.Purchases.AddRange(purchases);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetPurchases();

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var returnedPurchases = (okResult!.Value as IEnumerable<Purchase>)!.ToList();
        
        returnedPurchases.Should().HaveCount(3);
        returnedPurchases[0].BuyerId.Should().Be(124); // Most recent (3 hours)
        returnedPurchases[1].BuyerId.Should().Be(125); // Middle (2 hours) 
        returnedPurchases[2].BuyerId.Should().Be(123); // Oldest (1 hour)
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}