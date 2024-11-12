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

                // Tính phí chăm sóc
                decimal takeCareFee = CalculateConsignmentFee(consignmentDateFrom, consignmentDateTo);

                var newConsignment = new Consignment
                {
                    UserId = userID,
                    KoiTypeId = koitypeID,
                    KoiId = koiID,
                    ConsignmentType = consignmentType,
                    Status = status,
                    ConsignmentPrice = consignmentPrice, // Giá gốc ký gửi, không bao gồm phí chăm sóc
                    TakeCareFee = takeCareFee,           // Gán phí chăm sóc vào thuộc tính TakeCareFee
                    ConsignmentDateFrom = consignmentDateFrom,
                    ConsignmentDateTo = consignmentDateTo,
                    UserImage = userImage,
                    ConsignmentTitle = consignmentTitle,
                    ConsignmentDetail = consignmentDetail
                };

                await _context.Consignments.AddAsync(newConsignment);
                await _context.SaveChangesAsync();

                // Map entity vừa tạo sang DTO
                var consignmentDto = _mapper.Map<ConsignmentDto>(newConsignment);
                consignmentDto.TakeCareFee = takeCareFee; // Trả TakeCareFee về cho người dùng
                return consignmentDto;
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
                    Price = consignmentPrice,  // Set the Price for Koi based on consignmentPrice
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

        public async Task<bool> UpdateConsignmentOrderStatusAsync(int consignmentId, string status)
        {
            try
            {
                // Retrieve the existing consignment along with its associated Koi
                var existingConsignment = await _context.Consignments
                    .Include(c => c.Koi)
                    .FirstOrDefaultAsync(c => c.ConsignmentId == consignmentId);

                if (existingConsignment == null || existingConsignment.Koi == null)
                {
                    return false; // Consignment or associated Koi not found
                }

                // Update the status in the Consignment table
                existingConsignment.Status = status;

                // Update the status in the Koi table based on Consignment status
                if (status.Equals("approved", StringComparison.OrdinalIgnoreCase))
                {
                    existingConsignment.Koi.Status = "available";
                }
                else
                {
                    // Set Koi status to unavailable if consignment is not approved
                    existingConsignment.Koi.Status = "unavailable";
                }

                // Save changes to the database
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update consignment order status: " + ex.Message);
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

        public async Task<ConsignmentDto> CreateConsignmentTakeCareOutsideShopAsync(int userId, ConsignmentTakeCareOutsideRequestDto request)
        {
            // Validate user existence
            if (!await _context.Users.AnyAsync(u => u.UserId == userId))
                throw new KeyNotFoundException("User not found.");

            // Validate KoiType existence
            if (!await _context.KoiTypes.AnyAsync(k => k.KoiTypeId == request.KoiTypeId))
                throw new KeyNotFoundException("Koi type not found.");

            // Create new Koi entry
            var newKoi = new Koi
            {
                KoiTypeId = request.KoiTypeId,
                Name = request.Name,
                Origin = request.Origin,
                Gender = request.Gender,
                Age = request.Age,
                Size = request.Size,
                Breed = request.Breed,
                Personality = request.Personality,
                FeedingAmount = request.FeedingAmount,
                FilterRate = request.FilterRate,
                HealthStatus = request.HealthStatus,
                AwardCertificates = request.AwardCertificates,
                Status = "unavailable",
                IsConsigned = true,
                Description = request.Description,
                DetailDescription = request.DetailDescription,
                ImageKoi = request.ImageKoi,
                ImageCertificate = request.ImageCertificate,
                AdditionImage = request.AdditionImage
            };

            _context.Kois.Add(newKoi);
            await _context.SaveChangesAsync();

            // Calculate TakeCareFee
            var consignmentDateFrom = DateTime.Now;
            var consignmentDateTo = request.ConsignmentDateTo;
            decimal takeCareFee = CalculateTakeCareFee(consignmentDateFrom, consignmentDateTo);

            // Create consignment linked to new Koi
            var newConsignment = new Consignment
            {
                UserId = userId,
                KoiTypeId = request.KoiTypeId,
                KoiId = newKoi.KoiId,
                ConsignmentType = request.ConsignmentType,
                Status = "awaiting inspection",
                ConsignmentDateFrom = consignmentDateFrom,
                ConsignmentDateTo = consignmentDateTo,
                ConsignmentTitle = request.ConsignmentTitle,
                ConsignmentDetail = request.ConsignmentDetail,
                TakeCareFee = takeCareFee // Assign the calculated fee
            };

            _context.Consignments.Add(newConsignment);
            await _context.SaveChangesAsync();

            return _mapper.Map<ConsignmentDto>(newConsignment);
        }

        private decimal CalculateTakeCareFee(DateTime consignmentDateFrom, DateTime consignmentDateTo)
        {
            TimeSpan duration = consignmentDateTo - consignmentDateFrom;
            int totalDays = (int)duration.TotalDays;

            // Calculate the fee based on full months and remaining weeks
            int months = totalDays / 30;
            int remainingDays = totalDays % 30;
            int weeks = remainingDays / 7;

            decimal fee = (months * 100000) + (weeks * 70000);
            return fee;
        }



        public async Task<bool> UpdateConsignmentTakeCareOutsideShopAsync(int consignmentId, int userId, UpdateConsignmentTakeCareOutsideRequestDto request)
        {
            try
            {
                // Retrieve the existing consignment and associated Koi by ID and user
                var existingConsignment = await _context.Consignments
                    .Include(c => c.Koi)
                    .FirstOrDefaultAsync(c => c.ConsignmentId == consignmentId && c.UserId == userId);

                if (existingConsignment == null || existingConsignment.Koi == null)
                {
                    return false; // Consignment or associated Koi not found
                }

                // Update consignment details (linked to consignment)
                existingConsignment.ConsignmentDateTo = request.ConsignmentDateTo ?? existingConsignment.ConsignmentDateTo;
                existingConsignment.ConsignmentTitle = request.ConsignmentTitle ?? existingConsignment.ConsignmentTitle;
                existingConsignment.ConsignmentDetail = request.ConsignmentDetail ?? existingConsignment.ConsignmentDetail;

                // Update koi details (linked to koiId)
                existingConsignment.Koi.Name = request.Name ?? existingConsignment.Koi.Name;
                existingConsignment.Koi.Origin = request.Origin ?? existingConsignment.Koi.Origin;
                existingConsignment.Koi.Gender = request.Gender ?? existingConsignment.Koi.Gender;
                existingConsignment.Koi.Age = request.Age ?? existingConsignment.Koi.Age;
                existingConsignment.Koi.Size = request.Size ?? existingConsignment.Koi.Size;
                existingConsignment.Koi.Breed = request.Breed ?? existingConsignment.Koi.Breed;
                existingConsignment.Koi.Personality = request.Personality ?? existingConsignment.Koi.Personality;
                existingConsignment.Koi.FeedingAmount = request.FeedingAmount ?? existingConsignment.Koi.FeedingAmount;
                existingConsignment.Koi.FilterRate = request.FilterRate ?? existingConsignment.Koi.FilterRate;
                existingConsignment.Koi.HealthStatus = request.HealthStatus ?? existingConsignment.Koi.HealthStatus;
                existingConsignment.Koi.AwardCertificates = request.AwardCertificates ?? existingConsignment.Koi.AwardCertificates;
                existingConsignment.Koi.Description = request.Description ?? existingConsignment.Koi.Description;
                existingConsignment.Koi.DetailDescription = request.DetailDescription ?? existingConsignment.Koi.DetailDescription;
                existingConsignment.Koi.ImageKoi = request.ImageKoi ?? existingConsignment.Koi.ImageKoi;
                existingConsignment.Koi.ImageCertificate = request.ImageCertificate ?? existingConsignment.Koi.ImageCertificate;
                existingConsignment.Koi.AdditionImage = request.AdditionImage ?? existingConsignment.Koi.AdditionImage;

                // Save changes to the database
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update consignment: " + ex.Message);
            }
        }



        //    public async Task<bool> UpdateConsignmentTakeCareOutsideShopAsync(
        //int consignmentId,
        //int userId,
        //int koiTypeId,
        //string name,
        //string origin,
        //string gender,
        //int age,
        //decimal size,
        //string breed,
        //string personality,
        //decimal feedingAmount,
        //decimal filterRate,
        //string healthStatus,
        //string awardCertificates,
        //string description,
        //string detailDescription,
        //string imageKoi,
        //string imageCertificate,
        //string additionImage,
        //DateTime consignmentDateTo,
        //string? consignmentTitle,
        //string? consignmentDetail)
        //    {
        //        try
        //        {
        //            // Retrieve the existing consignment and koi details
        //            var existingConsignment = await _context.Consignments
        //                .Include(c => c.Koi)
        //                .FirstOrDefaultAsync(c => c.ConsignmentId == consignmentId && c.UserId == userId);

        //            if (existingConsignment == null || existingConsignment.Koi == null)
        //            {
        //                return false; // Consignment or Koi not found
        //            }

        //            // Update Koi details
        //            var koiToUpdate = existingConsignment.Koi;
        //            koiToUpdate.KoiTypeId = koiTypeId;
        //            koiToUpdate.Name = name;
        //            koiToUpdate.Origin = origin;
        //            koiToUpdate.Gender = gender;
        //            koiToUpdate.Age = age;
        //            koiToUpdate.Size = size;
        //            koiToUpdate.Breed = breed;
        //            koiToUpdate.Personality = personality;
        //            koiToUpdate.FeedingAmount = feedingAmount;
        //            koiToUpdate.FilterRate = filterRate;
        //            koiToUpdate.HealthStatus = healthStatus;
        //            koiToUpdate.AwardCertificates = awardCertificates;
        //            koiToUpdate.Description = description;
        //            koiToUpdate.DetailDescription = detailDescription;
        //            koiToUpdate.ImageKoi = imageKoi;
        //            koiToUpdate.ImageCertificate = imageCertificate;
        //            koiToUpdate.AdditionImage = additionImage;

        //            // Update Consignment details
        //            existingConsignment.KoiTypeId = koiTypeId;
        //            existingConsignment.ConsignmentDateTo = consignmentDateTo;
        //            existingConsignment.ConsignmentTitle = consignmentTitle ?? existingConsignment.ConsignmentTitle;
        //            existingConsignment.ConsignmentDetail = consignmentDetail ?? existingConsignment.ConsignmentDetail;

        //            // Save changes to the database
        //            await _context.SaveChangesAsync();

        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            throw new Exception("Failed to update consignment: " + ex.Message);
        //        }
        //    }






        public async Task<ConsignmentDto> CreateConsignmentOrderFromOutsideShopAsync(int userId, ConsignmentOrderRequestDto request)
        {
            // Step 1: Create a new Koi entry for the consignment
            var newKoi = new Koi
            {
                KoiTypeId = request.KoiTypeId,
                Name = request.Name,
                Origin = request.Origin,
                Gender = request.Gender,
                Age = request.Age,
                Size = request.Size,
                Breed = request.Breed,
                Personality = request.Personality,
                FeedingAmount = request.FeedingAmount,
                FilterRate = request.FilterRate,
                HealthStatus = request.HealthStatus,
                AwardCertificates = request.AwardCertificates,
                Status = "unavailable", // Initially unavailable until approved
                quantityInStock = 1, // For consigned items, quantity is 1
                IsConsigned = true, // Mark as consigned
                Description = request.Description,
                DetailDescription = request.DetailDescription,
                ImageKoi = request.ImageKoi,
                ImageCertificate = request.ImageCertificate,
                AdditionImage = request.AdditionImage,
                Price = request.ConsignmentPrice // Synchronize Price with consignmentPrice
            }; 

            // Save the new Koi entry to the database
            _context.Kois.Add(newKoi);
            await _context.SaveChangesAsync();

            // Step 2: Create a Consignment entry linked to the new Koi entry
            var newConsignment = new Consignment
            {
                UserId = userId,
                KoiTypeId = request.KoiTypeId,
                KoiId = newKoi.KoiId, // Use the newly generated KoiId
                ConsignmentType = request.ConsignmentType,
                Status = "awaiting inspection", // Initial status
                ConsignmentDateFrom = DateTime.Now,
                ConsignmentPrice = request.ConsignmentPrice, // Save consignment price
                ConsignmentTitle = request.ConsignmentTitle,
                ConsignmentDetail = request.ConsignmentDetail
            };

            // Save the new Consignment entry to the database
            _context.Consignments.Add(newConsignment);
            await _context.SaveChangesAsync();

            // Map and return the created consignment as DTO
            return _mapper.Map<ConsignmentDto>(newConsignment);
        }





        public async Task<bool> UpdateConsignmentTakeCareInsideShopAsync(
   int consignmentId,
   int userId,
   int koiTypeId,
   int koiId,
   DateTime consignmentDateTo,
   string? userImage,
   string? consignmentTitle,
   string? consignmentDetail
)
        {
            try
            {
                // Retrieve the existing consignment by ID
                var existingConsignment = await _context.Consignments
                    .FirstOrDefaultAsync(c => c.ConsignmentId == consignmentId && c.UserId == userId);

                if (existingConsignment == null)
                {
                    return false; // Consignment not found or not authorized
                }

                // Update relevant fields
                existingConsignment.KoiTypeId = koiTypeId;
                existingConsignment.KoiId = koiId;
                existingConsignment.ConsignmentDateTo = consignmentDateTo;
                existingConsignment.UserImage = userImage ?? existingConsignment.UserImage;
                existingConsignment.ConsignmentTitle = consignmentTitle ?? existingConsignment.ConsignmentTitle;
                existingConsignment.ConsignmentDetail = consignmentDetail ?? existingConsignment.ConsignmentDetail;

                // Save changes
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update consignment: " + ex.Message);
            }
        }
        public async Task<bool> UpdateConsignmentOrderAsync(
    int consignmentId,
    int userId,
    decimal consignmentPrice,
    string? consignmentTitle,
    string? consignmentDetail)
        {
            // Lấy thông tin consignment dựa trên consignmentId và userId
            var existingConsignment = await _context.Consignments
                .FirstOrDefaultAsync(c => c.ConsignmentId == consignmentId && c.UserId == userId);

            if (existingConsignment == null)
            {
                return false; // Không tìm thấy consignment
            }

            // Cập nhật các trường cần thiết trong bảng Consignment
            existingConsignment.ConsignmentPrice = consignmentPrice;
            existingConsignment.ConsignmentTitle = consignmentTitle ?? existingConsignment.ConsignmentTitle;
            existingConsignment.ConsignmentDetail = consignmentDetail ?? existingConsignment.ConsignmentDetail;

            // Lấy thông tin Koi liên quan để cập nhật giá
            var existingKoi = await _context.Kois.FirstOrDefaultAsync(k => k.KoiId == existingConsignment.KoiId);
            if (existingKoi == null)
            {
                throw new KeyNotFoundException("Không tìm thấy cá Koi liên quan.");
            }

            // Cập nhật giá trong bảng Koi để đồng bộ với consignmentPrice
            existingKoi.Price = consignmentPrice;

            // Lưu thay đổi vào cơ sở dữ liệu
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<bool> UpdateConsignmentOrderFromOutsideShopAsync(
     int consignmentId, int userId, UpdateConsignmentOrderRequestDto request)
        {
            var existingConsignment = await _context.Consignments
                .Include(c => c.Koi)
                .FirstOrDefaultAsync(c => c.ConsignmentId == consignmentId && c.UserId == userId);

            if (existingConsignment == null || existingConsignment.Koi == null)
            {
                return false; // Consignment or associated Koi not found
            }

            // Update consignment details
            existingConsignment.ConsignmentPrice = request.ConsignmentPrice;
            existingConsignment.ConsignmentTitle = request.ConsignmentTitle ?? existingConsignment.ConsignmentTitle;
            existingConsignment.ConsignmentDetail = request.ConsignmentDetail ?? existingConsignment.ConsignmentDetail;

            // Update Koi details, including Price
            existingConsignment.Koi.Price = request.ConsignmentPrice; // Sync price
            existingConsignment.Koi.Name = request.Name ?? existingConsignment.Koi.Name;
            existingConsignment.Koi.Origin = request.Origin ?? existingConsignment.Koi.Origin;
            existingConsignment.Koi.Gender = request.Gender ?? existingConsignment.Koi.Gender;
            existingConsignment.Koi.Age = request.Age ?? existingConsignment.Koi.Age;
            existingConsignment.Koi.Size = request.Size ?? existingConsignment.Koi.Size;
            existingConsignment.Koi.Breed = request.Breed ?? existingConsignment.Koi.Breed;
            existingConsignment.Koi.Personality = request.Personality ?? existingConsignment.Koi.Personality;
            existingConsignment.Koi.FeedingAmount = request.FeedingAmount ?? existingConsignment.Koi.FeedingAmount;
            existingConsignment.Koi.FilterRate = request.FilterRate ?? existingConsignment.Koi.FilterRate;
            existingConsignment.Koi.HealthStatus = request.HealthStatus ?? existingConsignment.Koi.HealthStatus;
            existingConsignment.Koi.AwardCertificates = request.AwardCertificates ?? existingConsignment.Koi.AwardCertificates;
            existingConsignment.Koi.Description = request.Description ?? existingConsignment.Koi.Description;
            existingConsignment.Koi.DetailDescription = request.DetailDescription ?? existingConsignment.Koi.DetailDescription;
            existingConsignment.Koi.ImageKoi = request.ImageKoi ?? existingConsignment.Koi.ImageKoi;
            existingConsignment.Koi.ImageCertificate = request.ImageCertificate ?? existingConsignment.Koi.ImageCertificate;
            existingConsignment.Koi.AdditionImage = request.AdditionImage ?? existingConsignment.Koi.AdditionImage;

            // Save changes to the database
            await _context.SaveChangesAsync();
            return true;
        }


        private decimal CalculateConsignmentFee(DateTime consignmentDateFrom, DateTime consignmentDateTo)
        {
            var totalDays = (consignmentDateTo - consignmentDateFrom).Days;

            // Tính số tháng và tuần
            var months = totalDays / 30;
            var weeks = (totalDays % 30) / 7;

            // Phí: 100,000 VND mỗi tháng và 70,000 VND mỗi tuần
            decimal fee = (months * 100000) + (weeks * 70000);

            return fee;
        }





    }
}