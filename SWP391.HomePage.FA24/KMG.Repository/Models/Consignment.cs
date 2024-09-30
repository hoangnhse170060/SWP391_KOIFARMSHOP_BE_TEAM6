using System;
using System.Collections.Generic;

namespace KMG.Repository.Models;

public partial class Consignment
{
    public int ConsignmentId { get; set; }

    public int? UserId { get; set; }

    public int? KoiId { get; set; }

    public string? ConsignmentType { get; set; }

    public string? Status { get; set; }

    public decimal? ConsignmentPrice { get; set; }

    public DateOnly? ConsignmentDate { get; set; }

    public byte[]? UserImage { get; set; }

    public virtual Koi? Koi { get; set; }

    public virtual User? User { get; set; }
}
