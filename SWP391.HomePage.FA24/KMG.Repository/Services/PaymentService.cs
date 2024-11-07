using System;
using System.Threading.Tasks;
using KMG.Repository.Interfaces;
using KMG.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace KMG.Repository.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly SwpkoiFarmShopContext _context;

        public PaymentService(SwpkoiFarmShopContext context)
        {
            _context = context;
        }

        public async Task<bool> ProcessPaymentAsync(int userId, int consignmentId, string paymentMethod, string shippingAddress, decimal discount)
        {
            // Kiểm tra consignment
            var consignment = await _context.Consignments.FirstOrDefaultAsync(c => c.ConsignmentId == consignmentId && c.Status == "available" && c.ConsignmentType == "online");
            if (consignment == null)
            {
                return false; // Consignment không tồn tại hoặc không khả dụng
            }

            // Tính tổng tiền sau khi giảm giá (nếu có)
            decimal totalAmount = (consignment.ConsignmentPrice ?? 0) - discount;

            // Tạo một bản ghi OrderConsignment
            var order = new OrderConsignment
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                ShippingAddress = shippingAddress,
                TotalAmount = totalAmount,
                Discount = discount,
                Status = "completed",
                ShippingStatus = "in transit"
            };

            _context.OrderConsignments.Add(order);
            await _context.SaveChangesAsync();

            // Tạo bản ghi chi tiết đơn hàng (OrderDetailConsignment)
            var orderDetail = new OrderDetailConsignment
            {
                OrderConsignmentId = order.OrderConsignmentId,
                ConsignmentId = consignmentId,
                Quantity = 1, // Thanh toán trực tiếp nên số lượng là 1
                Price = consignment.ConsignmentPrice ?? 0
            };

            _context.OrderDetailConsignments.Add(orderDetail);

            // Cập nhật trạng thái consignment thành 'sold' trước khi lưu thay đổi
            consignment.Status = "sold";

            // Lưu thay đổi bao gồm OrderDetailConsignment và cập nhật consignment
            await _context.SaveChangesAsync();

            return true;
        }

    }
}
