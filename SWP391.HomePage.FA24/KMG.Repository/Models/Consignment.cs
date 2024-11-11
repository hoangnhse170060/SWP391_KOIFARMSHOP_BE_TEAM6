using System;
using System.Collections.Generic;

namespace KMG.Repository.Models;

public partial class Consignment
{
    public int ConsignmentId { get; set; }

    public int KoiTypeId { get; set; }

    public int? UserId { get; set; }

    public int? KoiId { get; set; }

    public string? ConsignmentType { get; set; }

    public string? Status { get; set; }

    public decimal? ConsignmentPrice { get; set; }

    public DateTime? ConsignmentDateFrom { get; set; }
    public DateTime? ConsignmentDateTo { get; set; }

    public string? UserImage { get; set; }
    public string? ConsignmentTitle { get; set; }
    public string? ConsignmentDetail { get; set; }
    public int? BuyerId { get; set; }

    public decimal TakeCareFee { get; set; } = 0; 
    public virtual User? Buyer { get; set; } 

    public virtual Koi? Koi { get; set; }

    public virtual User? User { get; set; }
}
