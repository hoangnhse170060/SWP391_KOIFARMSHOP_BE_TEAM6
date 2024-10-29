using AutoMapper;
using KMG.Repository.Dto;
using KMG.Repository.Interfaces;
using KMG.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KMG.Repository.Services
{
    public class ConsignmentService : IConsignmentService
    {
        private readonly SwpkoiFarmShopContext _context;
        private readonly IMapper _mapper; // Inject AutoMapper

        public ConsignmentService(SwpkoiFarmShopContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        // Create Consignment
        public async Task<ConsignmentDto> CreateConsignmentAsync(int userID, int koiID, string consignmentType, string status, decimal consignmentPrice, DateTime consignmentDateFrom, DateTime consignmentDateTo, string userImage, string consignmentTitle, string consignmentDetail)
        {
            try
            {
                // Lấy thông tin người dùng từ UserID
                var user = await _context.Users.FindAsync(userID);
                if (user == null)
                {
                    throw new KeyNotFoundException("User not found.");
                }

                // Nếu là customer, status mặc định là 'awaiting inspection'
                if (user.Role == "customer")
                {
                    status = "awaiting inspection";
                }

                var newConsignment = new Consignment
                {
                    UserId = userID,               
                    KoiId = koiID,                  
                    ConsignmentType = consignmentType,
                    Status = status,              
                    ConsignmentPrice = consignmentPrice,
                    ConsignmentDateFrom = consignmentDateFrom,
                    ConsignmentDateTo = consignmentDateTo,
                    UserImage = userImage,
                    ConsignmentTitle = consignmentTitle,
                    ConsignmentDetail = consignmentDetail
                };

                await _context.Consignments.AddAsync(newConsignment);
                await _context.SaveChangesAsync();

                // Map entity vừa tạo sang DTO
                return _mapper.Map<ConsignmentDto>(newConsignment);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to create consignment: " + ex.Message);
            }
        }


        // Update Consignment
        public async Task<bool> UpdateConsignmentAsync(int consignmentId, int userID, int koiID, string consignmentType, string status, decimal consignmentPrice, DateTime consignmentDateFrom, DateTime consignmentDateTo, string userImage, string consignmentTitle, string consignmentDetail)
        {
            try
            {
                // Lấy thông tin consignment từ ID
                var existingConsignment = await _context.Consignments.FindAsync(consignmentId);
                if (existingConsignment == null)
                {
                    throw new KeyNotFoundException("Consignment not found.");
                }

                // Lấy thông tin người dùng từ UserID
                var user = await _context.Users.FindAsync(userID);
                if (user == null)
                {
                    throw new KeyNotFoundException("User not found.");
                }

                // Kiểm tra vai trò người dùng
                if (user.Role == "customer")
                {
                    // Khách hàng không được chỉnh sửa status
                    status = existingConsignment.Status;
                }

                // Cập nhật thông tin consignment
                existingConsignment.UserId = userID;
                existingConsignment.KoiId = koiID;
                existingConsignment.ConsignmentType = consignmentType;
                existingConsignment.Status = status;    
                existingConsignment.ConsignmentPrice = consignmentPrice;
                existingConsignment.ConsignmentDateFrom = consignmentDateFrom;
                existingConsignment.ConsignmentDateTo = consignmentDateTo;
                existingConsignment.ConsignmentDetail = consignmentDetail;
                existingConsignment.ConsignmentTitle = consignmentTitle;
                existingConsignment.UserImage = userImage;

                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update consignment: " + ex.Message);
            }
        }


        // Delete Consignment
        public async Task<bool> DeleteConsignmentAsync(int consignmentId)
        {
            try
            {
                var existingConsignment = await _context.Consignments.FindAsync(consignmentId);
                if (existingConsignment == null)
                {
                    throw new KeyNotFoundException("Consignment not found.");
                }

                _context.Consignments.Remove(existingConsignment);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to delete consignment: " + ex.Message);
            }
        }

        // Get Consignment by ID
        public async Task<ConsignmentDto> GetConsignmentByIdAsync(int consignmentId)
        {
            try
            {
                var consignment = await _context.Consignments.FirstOrDefaultAsync(c => c.ConsignmentId == consignmentId);
                if (consignment == null)
                {
                    throw new KeyNotFoundException("Consignment not found.");
                }

                // Map entity to DTO
                return _mapper.Map<ConsignmentDto>(consignment);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get consignment: " + ex.Message);
            }
        }

        // Get All Consignments
        public async Task<IEnumerable<ConsignmentDto>> GetAllConsignmentsAsync()
        {
            try
            {
                var consignments = await _context.Consignments.ToListAsync();

                // Map the list of entities to DTOs
                return _mapper.Map<IEnumerable<ConsignmentDto>>(consignments);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get consignments: " + ex.Message);
            }
        }
        // Triển khai phương thức CreateConsignmentsFromOrdersAsync
        public async Task<IEnumerable<ConsignmentDto>> CreateConsignmentsFromOrdersAsync(int userId)
        {
            // Lấy danh sách tất cả các đơn hàng của user mà không cần kiểm tra OrderStatus và DeliveryStatus
            var orders = await _context.PurchaseHistories
                .Where(o => o.UserId == userId)
                .ToListAsync();

            var consignments = new List<Consignment>();

            foreach (var order in orders)
            {
                var consignment = new Consignment
                {
                    UserId = userId,
                    KoiId = order.OrderId, // Sử dụng OrderId làm KoiId; điều chỉnh nếu cần thiết
                    ConsignmentType = "online",
                    Status = "awaiting inspection",
                    ConsignmentPrice = order.FinalMoney,
                    ConsignmentDateFrom = order.PurchaseDate?.ToDateTime(TimeOnly.MinValue), // Chuyển đổi từ DateOnly? sang DateTime?
                    ConsignmentDateTo = DateTime.Now.AddMonths(1), // Ví dụ: consignments trong 1 tháng
                    UserImage = null,
                    ConsignmentTitle = $"Consignment for Order {order.OrderId}",
                    ConsignmentDetail = $"Auto-generated consignment for order on {order.PurchaseDate}"
                };

                consignments.Add(consignment);
                await _context.Consignments.AddAsync(consignment);
            }

            await _context.SaveChangesAsync();
            return _mapper.Map<IEnumerable<ConsignmentDto>>(consignments);
        }

    }
}
