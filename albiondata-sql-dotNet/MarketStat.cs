using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace albiondata_sql_dotNet
{
  public class MarketStat
  {
    [Column("id")]
    public long Id { get; set; }

    [Column("item_id")]
    public string ItemId { get; set; }

    [Column("location")]
    public string Location { get; set; }

    [Column("price_avg")]
    public float PriceAverage { get; set; }

    [Column("price_max")]
    public int PriceMax { get; set; }

    [Column("price_min")]
    public int PriceMin { get; set; }

    [Column("timestamp")]
    public DateTime TimeStamp { get; set; }
  }
}
