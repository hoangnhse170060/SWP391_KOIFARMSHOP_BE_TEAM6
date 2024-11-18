using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace KMG.Repository.Models;

public partial class User
{
    public int UserId { get; set; }

    public string UserName { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? Role { get; set; }

    public string? Status { get; set; }
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }

    public DateOnly? RegisterDate { get; set; }

    public int? TotalPoints { get; set; }
    [JsonIgnore]
    public virtual ICollection<Address> Addresses { get; set; } = new List<Address>();
    [JsonIgnore]
    public virtual ICollection<Consignment> Consignments { get; set; } = new List<Consignment>();
    [JsonIgnore]
    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
    [JsonIgnore]
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    [JsonIgnore]
    public virtual ICollection<Points> Points { get; set; } = new List<Points>();
    [JsonIgnore]
    public virtual ICollection<PurchaseHistory> PurchaseHistories { get; set; } = new List<PurchaseHistory>();
}
