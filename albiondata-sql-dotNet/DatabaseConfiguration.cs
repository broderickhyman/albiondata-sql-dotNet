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
      using (var context = new MainContext())
      {
        if (context.Database.EnsureCreated())
        {
          logger.LogInformation("Database Created");
          context.SaveChanges();
        }
        else
        {
          logger.LogInformation("Database Exists");
        }
      }
    }
  }
}
