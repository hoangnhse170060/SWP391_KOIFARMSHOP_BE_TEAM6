using System;
using System.Collections.Generic;

namespace KMG.Repository.Models;

public partial class User
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Role { get; set; }

    public string? Status { get; set; }

    public DateOnly? RegisterDate { get; set; }

    public int? TotalPoints { get; set; }

    public virtual ICollection<Consignment> Consignments { get; set; } = new List<Consignment>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Point> Points { get; set; } = new List<Point>();

    public virtual ICollection<PurchaseHistory> PurchaseHistories { get; set; } = new List<PurchaseHistory>();
}
