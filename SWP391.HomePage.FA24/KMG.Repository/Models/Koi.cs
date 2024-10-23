using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace KMG.Repository.Models;

public partial class Koi
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int KoiId { get; set; }

    public int? KoiTypeId { get; set; }
    public string? Name {  get; set; }
    public string? Origin { get; set; }

    public string? Gender { get; set; }

    public int? Age { get; set; }

    public decimal? Size { get; set; }
    [MaxLength(10)]
    [RegularExpression("purebred|F1 hybrid", ErrorMessage = "Invalid breed. Must be 'purebred' or 'F1 hybrid'.")]
    public string? Breed { get; set; }

    public string? Personality { get; set; }

    public decimal? FeedingAmount { get; set; }

    public decimal? FilterRate { get; set; }

    public string? HealthStatus { get; set; }

    public string? AwardCertificates { get; set; }

    public string? Status { get; set; }
    public decimal? Price { get; set; }
    public int QuantityInstock {  get; set; }

    public string? ImageKoi { get; set; }

    public string? ImageCertificate { get; set; }
    public string? Description { get; set; }
    public string? DetailDescription { get; set; } 
    public string? AdditionImage { get; set; }
    [JsonIgnore]
    public virtual ICollection<Consignment> Consignments { get; set; } = new List<Consignment>();
    [JsonIgnore]
    public virtual KoiType? KoiType { get; set; }
    [JsonIgnore]
    public virtual ICollection<OrderKoi> OrderKois { get; set; } = new List<OrderKoi>();
}
