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

    public void Dispose()
    {
        _context.Dispose();
    }
}