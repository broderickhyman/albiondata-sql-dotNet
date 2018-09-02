using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace albiondata_sql_dotNet
{
  public class MarketOrderContext : DbContext
  {
    public DbSet<MarketOrderDB> MarketOrders { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      DatabaseConfiguration.Configure(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<MarketOrderDB>(entity =>
      {
        entity.HasKey(e => e.Id);
        entity.HasAlternateKey(e => e.AlbionId)
              .HasName("AlbionId");

        entity.HasIndex(e => new { e.ItemTypeId, e.AuctionType, e.LocationId, e.UpdatedAt, e.DeletedAt })
              .HasName("Main");
        entity.HasIndex(e => new { e.ItemTypeId, e.UpdatedAt, e.DeletedAt })
              .HasName("TypeId");

        entity.Property(e => e.AlbionId)
              .IsRequired();
        entity.Property(e => e.Amount)
              .HasColumnName("amount");
        entity.Property(e => e.AuctionType)
              .HasColumnName("auction_type")
              .HasMaxLength(32);
        entity.Property(e => e.EnchantmentLevel)
              .HasColumnName("enchantment_level");
        entity.Property(e => e.Expires)
              .HasColumnName("expires");
        entity.Property(e => e.Id)
              .HasColumnName("id");
        entity.Property(e => e.ItemGroupTypeId)
              .HasColumnName("group_id")
              .HasMaxLength(128);
        entity.Property(e => e.ItemTypeId)
              .HasColumnName("item_id")
              .HasMaxLength(128);
        entity.Property(e => e.LocationId)
              .HasColumnName("location")
              .IsRequired();
        entity.Property(e => e.QualityLevel)
              .HasColumnName("quality_level");
        entity.Property(e => e.UnitPriceSilver)
              .HasColumnName("price");
      });
    }
  }

  public class MarketOrderDB : MarketOrderUpload
  {
    [Column("albion_id")]
    public long AlbionId { get; set; }

    [Column("initial_amount")]
    public long InitialAmount { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    public override string ToString()
    {
      return $"{AlbionId} - {ItemTypeId} at {LocationId}";
    }
  }

  public class MarketOrderUpload
  {
    public long Id { get; set; }
    public string ItemTypeId { get; set; }
    public string ItemGroupTypeId { get; set; }
    public int LocationId { get; set; }
    public int QualityLevel { get; set; }
    public int EnchantmentLevel { get; set; }
    public long UnitPriceSilver { get; set; }
    public long Amount { get; set; }
    public string AuctionType { get; set; }
    public DateTime Expires { get; set; }

    public override string ToString()
    {
      return $"{Id}{Amount}";
    }
  }
}
