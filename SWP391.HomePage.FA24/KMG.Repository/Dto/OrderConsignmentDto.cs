using System;
using System.Collections.Generic;

namespace KMG.Repository.Dto
{
    public class OrderConsignmentDto
    {
        public int OrderConsignmentId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentMethod { get; set; }
        public string ShippingAddress { get; set; }
        public decimal Discount { get; set; }
        public string Status { get; set; }
        public string ShippingStatus { get; set; }
        public List<OrderDetailConsignmentDto> OrderDetails { get; set; }
    }
}
