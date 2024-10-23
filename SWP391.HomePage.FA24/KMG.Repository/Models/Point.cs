using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KMG.Repository.Models;

public partial class Point
{
    public int PointsId { get; set; }

    public int? UserId { get; set; }

    public int? TotalPoints { get; set; }
    [JsonIgnore]
    public virtual User? User { get; set; }
}
