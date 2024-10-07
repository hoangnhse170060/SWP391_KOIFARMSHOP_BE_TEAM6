using System;
using System.Collections.Generic;

namespace KMG.Repository.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int? UserId { get; set; }

    public int? PromotionId { get; set; }

    public DateOnly? OrderDate { get; set; }

    public decimal? TotalMoney { get; set; }

    public decimal? DiscountMoney { get; set; }

    public decimal? FinalMoney { get; set; }

    public int? UsedPoints { get; set; }

    public int? EarnedPoints { get; set; }

    public string? OrderStatus { get; set; }

    public string? PaymentMethod { get; set; }

    public DateOnly? ShippingDate { get; set; }

    public string? DeliveryStatus { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<OrderFish> OrderFishes { get; set; } = new List<OrderFish>();

    public virtual ICollection<OrderKoi> OrderKois { get; set; } = new List<OrderKoi>();

    public virtual Promotion? Promotion { get; set; }

    public virtual ICollection<PurchaseHistory> PurchaseHistories { get; set; } = new List<PurchaseHistory>();

    public virtual User? User { get; set; }


}
