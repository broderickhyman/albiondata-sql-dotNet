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
  }
}
