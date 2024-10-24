using System;
using System.Collections.Generic;

namespace KMG.Repository.Models;

public partial class Point
{
    public int PointsId { get; set; }

    public int? UserId { get; set; }

    public int? TotalPoints { get; set; }

    public virtual User? User { get; set; }
}
