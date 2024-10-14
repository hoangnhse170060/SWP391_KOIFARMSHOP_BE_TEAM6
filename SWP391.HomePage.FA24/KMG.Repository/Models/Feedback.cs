using System;
using System.Collections.Generic;

namespace KMG.Repository.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int? UserId { get; set; }

    public int? OrderId { get; set; }

    public int? KoiId { get; set; }

    public int? FishesId { get; set; }

    public int? Rating { get; set; }

    public string? Content { get; set; }

    public DateOnly? FeedbackDate { get; set; }

    public virtual Order? Order { get; set; }

    public virtual User? User { get; set; }
}
