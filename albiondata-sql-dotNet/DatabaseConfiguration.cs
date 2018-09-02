using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace albiondata_sql_dotNet
{
  public static class DatabaseConfiguration
  {
    public static void Configure(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseMySQL(Program.SqlConnectionUrl);
    }

    public static void EnsureCreated()
    {
      var logger = Program.CreateLogger<Program>();
      using (var context = new MarketOrderContext())
      {
        if (context.Database.EnsureCreated())
        {
          logger.LogInformation("Market Order Table Created");
          context.SaveChanges();
        }
        else
        {
          logger.LogInformation("Market Order Table Exists");
        }
      }
      using (var context = new MarketStatContext())
      {
        if (context.Database.EnsureCreated())
        {
          logger.LogInformation("Market Stat Table Created");
          context.SaveChanges();
        }
        else
        {
          logger.LogInformation("Market Stat Table Exists");
        }
      }
      using (var context = new GoldPriceContext())
      {
        if (context.Database.EnsureCreated())
        {
          logger.LogInformation("Gold Price Table Created");
          context.SaveChanges();
        }
        else
        {
          logger.LogInformation("Gold Price Table Exists");
        }
      }
    }
  }
}
