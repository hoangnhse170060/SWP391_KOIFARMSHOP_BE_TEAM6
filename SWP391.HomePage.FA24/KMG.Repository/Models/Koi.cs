using System;
using System.Collections.Generic;

namespace KMG.Repository.Models;

public partial class Koi
{
    public int? Quantity;

    public int KoiId { get; set; }

    public int? KoiTypeId { get; set; }

    public string? Origin { get; set; }

    public string? Gender { get; set; }

    public int? Age { get; set; }

    public decimal? Size { get; set; }

    public string? Breed { get; set; }

    public string? Personality { get; set; }

    public decimal? FeedingAmount { get; set; }

    public decimal? FilterRate { get; set; }

    public string? HealthStatus { get; set; }

    public string? AwardCertificates { get; set; }

    public decimal? Price { get; set; }

    public byte[]? ImageKoi { get; set; }

    public byte[]? ImageCertificate { get; set; }

    public virtual ICollection<Consignment> Consignments { get; set; } = new List<Consignment>();

    public virtual KoiType? KoiType { get; set; }

    public virtual ICollection<OrderKoi> OrderKois { get; set; } = new List<OrderKoi>();
    public string Status { get; set; }
}
