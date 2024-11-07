namespace KMG.Repository.Models
{
    public class OrderDetailConsignment
    {
        public int OrderDetailConsignmentId { get; set; }
        public int OrderConsignmentId { get; set; }
        public int ConsignmentId { get; set; }
        public int Quantity { get; set; } = 1; // mặc định là 1 vì mỗi consignment chỉ có một đơn vị để bán
        public decimal Price { get; set; }

        // Quan hệ với OrderConsignment và Consignment
        public virtual OrderConsignment OrderConsignment { get; set; }
        public virtual Consignment Consignment { get; set; }
    }
}
