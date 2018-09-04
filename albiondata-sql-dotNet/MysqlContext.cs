using AlbionData.Models;
using Microsoft.EntityFrameworkCore;

namespace albiondata_sql_dotNet
{
  public class MysqlContext : MainContext
  {
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      optionsBuilder.UseMySQL(Program.SqlConnectionUrl);
    }
  }
}
