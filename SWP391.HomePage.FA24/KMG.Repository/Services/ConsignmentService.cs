using AutoMapper;
using KMG.Repository.Dto;
using KMG.Repository.Interfaces;
using KMG.Repository.Models;
using Microsoft.EntityFrameworkCore;

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
        public async Task<ConsignmentDto> CreateConsignmentAsync(int userID, int koitypeID, int? koiID, string consignmentType, string status, decimal consignmentPrice, DateTime consignmentDateFrom, DateTime consignmentDateTo, string userImage, string consignmentTitle, string consignmentDetail)
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
                    KoiTypeId = koitypeID,
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

        public async Task<ConsignmentDto> CreateConsignmentOrderAsync(int userId, int koiTypeId, int koiId, string consignmentType, decimal consignmentPrice, string? consignmentTitle, string? consignmentDetail)
        {
            // Step 1: Retrieve the existing Koi details by koiId and koiTypeId
            var existingKoi = await _context.Kois
                .FirstOrDefaultAsync(k => k.KoiId == koiId && k.KoiTypeId == koiTypeId);

            if (existingKoi == null)
            {
                throw new KeyNotFoundException("Koi with specified ID and Type not found.");
            }

            // Step 2: Create a new Koi entry with similar attributes (only if the consignment is online)
            int? newKoiId = null;
            if (consignmentType.ToLower() == "online")
            {
                var newKoi = new Koi
                {
                    KoiTypeId = existingKoi.KoiTypeId,
                    Name = existingKoi.Name,
                    Origin = existingKoi.Origin,
                    Gender = existingKoi.Gender,
                    Age = existingKoi.Age,
                    Size = existingKoi.Size,
                    Breed = existingKoi.Breed,
                    Personality = existingKoi.Personality,
                    FeedingAmount = existingKoi.FeedingAmount,
                    FilterRate = existingKoi.FilterRate,
                    HealthStatus = existingKoi.HealthStatus,
                    AwardCertificates = existingKoi.AwardCertificates,
                    Status = "unavailable",    // Initial status as unavailable until approved
                    Price = consignmentPrice,
                    quantityInStock = 1,       // Since this is a consigned item, quantity is 1
                    IsConsigned = true,        // Mark as consigned
                    Description = existingKoi.Description,
                    DetailDescription = existingKoi.DetailDescription,
                    ImageKoi = existingKoi.ImageKoi,
                    ImageCertificate = existingKoi.ImageCertificate,
                    AdditionImage = existingKoi.AdditionImage
                };

                _context.Kois.Add(newKoi);
                await _context.SaveChangesAsync();
                newKoiId = newKoi.KoiId; // Capture the new Koi ID for association with consignment
            }

            // Step 3: Create a new Consignment entry with the newly created Koi ID
            var newConsignment = new Consignment
            {
                UserId = userId,
                KoiTypeId = koiTypeId,
                KoiId = newKoiId,                 // Link to the newly created Koi ID
                ConsignmentType = consignmentType,
                Status = "awaiting inspection",   // Initial status
                ConsignmentPrice = consignmentPrice,
                ConsignmentDateFrom = DateTime.Now,
                ConsignmentTitle = consignmentTitle,
                ConsignmentDetail = consignmentDetail
            };

            _context.Consignments.Add(newConsignment);
            await _context.SaveChangesAsync();

            // Map and return the created consignment as DTO
            return _mapper.Map<ConsignmentDto>(newConsignment);
        }




        // Update Consignment
        public async Task<bool> UpdateConsignmentAsync(int consignmentId, int userID, int koitypeID, int koiID, string consignmentType, string status, decimal consignmentPrice, DateTime consignmentDateFrom, DateTime consignmentDateTo, string userImage, string consignmentTitle, string consignmentDetail)
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
                existingConsignment.KoiTypeId = koitypeID;
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

        // update cho manager và staff
        public async Task<bool> UpdateConsignmentStatusAsync(int consignmentId, string newStatus)
        {
            try
            {
                // Lấy thông tin consignment từ ID
                var existingConsignment = await _context.Consignments.FindAsync(consignmentId);
                if (existingConsignment == null)
                {
                    throw new KeyNotFoundException("Consignment not found.");
                }

                // Cập nhật chỉ trường status của consignment
                existingConsignment.Status = newStatus;

                // Lưu thay đổi vào cơ sở dữ liệu
                await _context.SaveChangesAsync();

                return true;
            }
            catch (DbUpdateException ex)
            {
                // Kiểm tra ngoại lệ bên trong để biết chi tiết lỗi
                if (ex.InnerException != null)
                {
                    throw new Exception("Failed to update consignment status: " + ex.InnerException.Message);
                }
                throw new Exception("Failed to update consignment status: " + ex.Message);
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
                var consignment = await _context.Consignments
                    .Include(c => c.Koi) // Include model Koi để lấy thông tin chi tiết cá Koi
                    .FirstOrDefaultAsync(c => c.ConsignmentId == consignmentId);

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
                var consignments = await _context.Consignments
                    .Include(c => c.Koi) // Include Koi data
                    .Include(c => c.User) // Include User data to get UserName
                    .ToListAsync();

                // Map entity list to DTO list
                var consignmentDtos = _mapper.Map<IEnumerable<ConsignmentDto>>(consignments);

                // Thêm UserName vào DTO nếu cần
                foreach (var consignmentDto in consignmentDtos)
                {
                    var consignmentEntity = consignments.FirstOrDefault(c => c.ConsignmentId == consignmentDto.ConsignmentId);
                    if (consignmentEntity?.User != null)
                    {
                        consignmentDto.UserName = consignmentEntity.User.UserName; // Assuming ConsignmentDto has a UserName property
                    }
                }

                return consignmentDtos;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get consignments: " + ex.Message);
            }
        }

        public async Task<IEnumerable<ConsignmentDto>> GetConsignmentsByUserNameAsync(string userName)
        {
            try
            {
                var consignments = await _context.Consignments
                    .Include(c => c.Koi)
                    .Include(c => c.User) // Bao gồm thông tin User
                    .Where(c => c.User.UserName == userName) // Lọc theo UserName
                    .ToListAsync();

                return _mapper.Map<IEnumerable<ConsignmentDto>>(consignments);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get consignments: " + ex.Message);
            }
        }

        public async Task<IEnumerable<ConsignmentDto>> GetConsignmentsByUserIdAsync(int userId)
        {
            try
            {
                var consignments = await _context.Consignments
                    .Include(c => c.Koi)
                    .Include(c => c.User) // Bao gồm thông tin User để lấy UserName
                    .Where(c => c.UserId == userId) // Lọc theo UserId
                    .ToListAsync();

                return _mapper.Map<IEnumerable<ConsignmentDto>>(consignments); // AutoMapper sẽ tự động ánh xạ UserName
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to get consignments by UserId: " + ex.Message);
            }
        }




    }
}