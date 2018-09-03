using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace albiondata_sql_dotNet
{
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

  public class GoldPriceUpload
  {
    public int[] Prices;
    public long[] TimeStamps;
  }
}
