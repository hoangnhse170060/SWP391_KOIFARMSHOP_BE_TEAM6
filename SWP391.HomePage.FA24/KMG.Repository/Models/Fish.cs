using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KMG.Repository.Models;

public partial class Fish
{
    public int? quantityInStock { get; set; }

    public int FishesId { get; set; }

    public int? Quantity { get; set; }

    public int? KoiTypeId { get; set; }
    public string? Name { get; set; }
    public string? Status { get; set; }
    public decimal? Price { get; set; } 

    public string? ImageFishes { get; set; }
    public string? Description { get; set; }
    public string? DetailDescription { get; set; }

    [JsonIgnore]
    public virtual KoiType? KoiType { get; set; }
    [JsonIgnore]
    public virtual ICollection<OrderFish> OrderFishes { get; set; } = new List<OrderFish>();
}
