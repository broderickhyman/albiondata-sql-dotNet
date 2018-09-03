using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace albiondata_sql_dotNet
{
  internal class MainContext : DbContext
  {
    public DbSet<GoldPrice> GoldPrices { get; set; }
    public DbSet<MarketOrderDB> MarketOrders { get; set; }
    public DbSet<MarketStat> MarketStats { get; set; }

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

      modelBuilder.Entity<MarketStat>(entity =>
      {
        entity.HasKey(e => e.Id);
        entity.HasAlternateKey(e => new { e.ItemId, e.Location, e.TimeStamp });
        entity.Property(e => e.ItemId).HasMaxLength(128);
      });

      modelBuilder.Entity<GoldPrice>(entity =>
      {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TimeStamp, e.DeletedAt });
      });
    }
  }
}
