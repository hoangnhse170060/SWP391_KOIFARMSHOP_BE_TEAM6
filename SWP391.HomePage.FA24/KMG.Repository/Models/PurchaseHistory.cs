using System;
using System.Collections.Generic;

namespace KMG.Repository.Models;

public partial class PurchaseHistory
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public decimal? TotalMoney { get; set; }

    public decimal? DiscountMoney { get; set; }

    public decimal? FinalMoney { get; set; }

    public string? OrderStatus { get; set; }

    public string? PaymentMethod { get; set; }

    public DateOnly? ShippingDate { get; set; }

    public string? DeliveryStatus { get; set; }

    public int? PromotionId { get; set; }

    public int? EarnedPoints { get; set; }

    public int? UsedPoints { get; set; }

    public virtual Order Order { get; set; } = null!;

    public virtual Promotion? Promotion { get; set; }

    public virtual User User { get; set; } = null!;
}
