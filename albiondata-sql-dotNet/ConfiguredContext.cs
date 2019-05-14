using AlbionData.Models;
using Microsoft.EntityFrameworkCore;

namespace albiondata_sql_dotNet
{
  public class ConfiguredContext : MainContext
  {
    public ConfiguredContext() { }

    public ConfiguredContext(DbContextOptions<MainContext> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      // TODO: Add other database providers here
      optionsBuilder.UseMySql(Program.SqlConnectionUrl);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<MarketOrderExpiredDB>(entity =>
      {
        entity.ToTable("market_orders_expired");
        entity.HasKey(e => e.Id);
        entity.HasAlternateKey(e => e.AlbionId)
              .HasName("AlbionId");

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
        //entity.Property(e => e.ItemGroupTypeId)
        //      .HasColumnName("group_id")
        //      .HasMaxLength(128);
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
}
