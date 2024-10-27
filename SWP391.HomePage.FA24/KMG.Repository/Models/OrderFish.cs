using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KMG.Repository.Models;

public partial class OrderFish
{
    public int OrderId { get; set; }

    public int FishesId { get; set; }

    public int? Quantity { get; set; }



    public virtual Fish? Fishes { get; set; } = null!;



    public virtual Order? Order { get; set; } = null!;
}
