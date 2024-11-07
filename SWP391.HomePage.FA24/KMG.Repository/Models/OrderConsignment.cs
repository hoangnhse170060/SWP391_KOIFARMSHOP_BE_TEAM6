using System;
using System.Collections.Generic;

namespace KMG.Repository.Models
{
    public class OrderConsignment
    {
        public int OrderConsignmentId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string ShippingAddress { get; set; }
        public decimal Discount { get; set; } = 0;
        public string Status { get; set; } = "pending payment"; // mặc định là 'pending payment'
        public string ShippingStatus { get; set; } = "in transit"; // mặc định là 'in transit'

        // Quan hệ với User và OrderDetailConsignments
        public virtual User User { get; set; }
        public virtual ICollection<OrderDetailConsignment> OrderDetailConsignments { get; set; }
    }
}
