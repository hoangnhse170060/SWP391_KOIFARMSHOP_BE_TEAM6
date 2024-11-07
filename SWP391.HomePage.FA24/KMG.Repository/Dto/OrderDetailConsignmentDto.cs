using System;

namespace KMG.Repository.Dto
{
    public class OrderDetailConsignmentDto
    {
        public int OrderDetailConsignmentId { get; set; }
        public int OrderConsignmentId { get; set; }
        public int ConsignmentId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}

