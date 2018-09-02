using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace albiondata_sql_dotNet
{
  public class GoldPriceContext : DbContext
  {
    public DbSet<GoldPrice> GoldPrices { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      DatabaseConfiguration.Configure(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      modelBuilder.Entity<GoldPrice>(entity =>
      {
        entity.HasKey(e => e.Id);
        entity.HasIndex(e => new { e.TimeStamp, e.DeletedAt });
      });
    }
  }

  public class GoldPrice
  {
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("deleted_at")]
    public DateTime? DeletedAt { get; set; }

    [Column("id")]
    public long Id { get; set; }

    [Column("price")]
    public int Price { get; set; }

    [Column("timestamp")]
    public DateTime TimeStamp { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
  }
}
