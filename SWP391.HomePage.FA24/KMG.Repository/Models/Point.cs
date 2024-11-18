using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KMG.Repository.Models;

public partial class Points
{
    public int TransactionId { get; set; } 

    public int? UserId { get; set; }

    public string TransactionType { get; set; } 

    public DateTime TransactionDate { get; set; } 

    public int PointsChanged { get; set; }

    public int NewTotalPoints { get; set; }

    public int? OrderId { get; set; }
    [JsonIgnore]
    public virtual User? User { get; set; }
}
