using Microsoft.EntityFrameworkCore;
using PurchaseService.Models;

namespace PurchaseService.Data;

public class PurchaseDbContext : DbContext
{
    public PurchaseDbContext(DbContextOptions<PurchaseDbContext> options) : base(options)
    {
    }

    public DbSet<Purchase> Purchases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.PurchaseId);
            entity.Property(e => e.PurchaseId).ValueGeneratedOnAdd();
            
            entity.Property(e => e.Amount)
                .HasPrecision(18, 2);
            
            entity.Property(e => e.Status)
                .HasDefaultValue("Pending");
            
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Configure BuyerDetails as JSON column
            entity.Property(e => e.BuyerDetails)
                .HasColumnType("jsonb")
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<BuyerDetails>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new BuyerDetails());

            entity.HasIndex(e => e.BuyerId);
            entity.HasIndex(e => e.OfferId);
            entity.HasIndex(e => e.Status);
        });
    }
}