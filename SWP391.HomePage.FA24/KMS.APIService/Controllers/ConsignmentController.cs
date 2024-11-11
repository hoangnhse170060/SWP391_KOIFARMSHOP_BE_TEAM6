using AutoMapper;
using KMG.Repository.Dto;
using KMG.Repository.Interfaces;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "manager, staff, customer")]
    public class ConsignmentController : ControllerBase
    {
        private readonly IConsignmentService _consignmentService;
        private readonly IMapper _mapper;  // Inject IMapper
        private readonly SwpkoiFarmShopContext _context;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;

        public ConsignmentController(IConsignmentService consignmentService, IMapper mapper, SwpkoiFarmShopContext context, IEmailService emailService, IUserService userService)
        {
            _consignmentService = consignmentService;
            _mapper = mapper;  // Assign IMapper to the private field
            _context = context;
            _emailService = emailService;
            _userService = userService;
        }

        // GET: api/consignment/get-consignments
        [HttpGet("get-consignments")]
        public async Task<IActionResult> GetAllConsignments()
        {
            var consignments = await _consignmentService.GetAllConsignmentsAsync();
            var consignmentsDto = _mapper.Map<IEnumerable<ConsignmentDto>>(consignments);
            return Ok(consignmentsDto);  // Return mapped DTOs
        }
        // GET: api/consignment/user/{userId}
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetConsignmentsByUserId(int userId)
        {
            var consignments = await _consignmentService.GetConsignmentsByUserIdAsync(userId);
            if (consignments == null || !consignments.Any())
            {
                return NotFound("No consignments found for the specified user.");
            }
            return Ok(consignments);
        }


        // GET: api/consignment/get-consignment/{id}
        [HttpGet("get-consignment/{consignmentId}")]
        public async Task<IActionResult> GetConsignmentById(int consignmentId)
        {
            var consignment = await _consignmentService.GetConsignmentByIdAsync(consignmentId);
            if (consignment == null)
            {
                return NotFound("Consignment not found.");
            }
            var consignmentDto = _mapper.Map<ConsignmentDto>(consignment);  // Map to DTO
            return Ok(consignmentDto);  // Return mapped DTO
        }

        // GET: api/consignment/get-consignments-by-user/{userName}
        [HttpGet("get-consignments-by-user/{userName}")]
        public async Task<IActionResult> GetConsignmentsByUserName(string userName)
        {
            try
            {
                var consignments = await _consignmentService.GetConsignmentsByUserNameAsync(userName);
                if (consignments == null || !consignments.Any())
                {
                    return NotFound("No consignments found for the specified user.");
                }

                return Ok(consignments); // Return list of ConsignmentDto
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }






        [Authorize(Roles = "customer")]
        [HttpPost("create-consignment-take-care-inside-shop")]
        public async Task<IActionResult> CreateConsignmentTakeCareInsideShop(
    int koitypeID,
    int koiID,
    string consignmentType,
    DateTime consignmentDateTo,
    string? userImage = null,
    string? consignmentTitle = null,
    string? consignmentDetail = null
)
        {
            try
            {
                // Validate consignmentDateTo to ensure it's not in the past
                if (consignmentDateTo < DateTime.Now)
                {
                    return BadRequest("Consignment date cannot be in the past.");
                }

                // Get the UserId from the claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated.");
                }

                // Parse the UserId
                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    return BadRequest("Invalid User ID.");
                }

                var status = "awaiting inspection";

                // Set a default consignment price if not specified (for inside shop care)
                decimal consignmentPrice = 0; // Default price for consignment take care inside shop

                // Create consignment using the service
                var createdConsignment = await _consignmentService.CreateConsignmentAsync(
                    userId, koitypeID, koiID, consignmentType, status, consignmentPrice, DateTime.Now, consignmentDateTo, userImage, consignmentTitle, consignmentDetail
                );

                // Return created consignment with CreatedAtAction
                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = createdConsignment.ConsignmentId }, new
                {
                    consignment = createdConsignment,
                });
            }
            catch (Exception ex)
            {
                // Log exception and return server error
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [Authorize(Roles = "customer")]
        [HttpPost("create-consignment-order-inside-shop")]
        public async Task<IActionResult> CreateConsignmentOrderInsideShopAsync(
    int koiTypeId,
    int koiId,
    string consignmentType,
    decimal consignmentPrice,
    string? consignmentTitle = null,
    string? consignmentDetail = null)
        {
            try
            {
                // Lấy UserId từ thông tin người dùng đã xác thực
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Người dùng chưa xác thực.");
                }

                // Gọi phương thức dịch vụ để tạo đơn ký gửi
                var consignmentDto = await _consignmentService.CreateConsignmentOrderAsync(
                    userId, koiTypeId, koiId, consignmentType, consignmentPrice, consignmentTitle, consignmentDetail);

                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = consignmentDto.ConsignmentId }, consignmentDto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server nội bộ: {ex.Message}");
            }
        }



        [Authorize(Roles = "customer")]
        [HttpPost("create-consignment-order-from-outside-shop")]
        public async Task<IActionResult> CreateConsignmentOrderFromOutsideShop(
    int koiTypeId,
    string name,
    string origin,
    string gender,
    int age,
    decimal size,
    string breed,
    string personality,
    decimal feedingAmount,
    decimal filterRate,
    string healthStatus,
    string awardCertificates,
    string description,
    string detailDescription,
    string imageKoi,
    string imageCertificate,
    string additionImage,
    string consignmentType,
    decimal consignmentPrice, 
    string consignmentTitle,
    string consignmentDetail)
        {
            try
            {
                // Retrieve the UserId from the authenticated user's claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Call the service method to create a consignment order
                var createdConsignment = await _consignmentService.CreateConsignmentOrderFromOutsideShopAsync(
                    userId, koiTypeId, name, origin, gender, age, size, breed, personality,
                    feedingAmount, filterRate, healthStatus, awardCertificates, description,
                    detailDescription, imageKoi, imageCertificate, additionImage, consignmentType,
                    consignmentPrice, consignmentTitle, consignmentDetail);

                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = createdConsignment.ConsignmentId }, createdConsignment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [Authorize(Roles = "customer")]
        [HttpPost("create-consignment-take-care-outside-shop")]
        public async Task<IActionResult> CreateConsignmentTakeCareOutsideShop(
    int koiTypeId, string name, string origin, string gender, int age, decimal size,
    string breed, string personality, decimal feedingAmount, decimal filterRate,
    string healthStatus, string awardCertificates, string description,
    string detailDescription, string imageKoi, string imageCertificate,
    string additionImage, string consignmentType, DateTime consignmentDateTo,
    string consignmentTitle, string consignmentDetail)
        {
            try
            {
                // Get the UserId from the claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Call service to create consignment and koi record
                var consignmentDto = await _consignmentService.CreateConsignmentTakeCareOutsideShopAsync(
                    userId, koiTypeId, name, origin, gender, age, size, breed, personality,
                    feedingAmount, filterRate, healthStatus, awardCertificates, description,
                    detailDescription, imageKoi, imageCertificate, additionImage,
                    consignmentType, consignmentDateTo, consignmentTitle, consignmentDetail);

                // Return the created consignment
                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = consignmentDto.ConsignmentId }, consignmentDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


      

        [Authorize(Roles = "customer")]
        [HttpPut("update-consignment-order-inside-shop/{consignmentId}")]
        public async Task<IActionResult> UpdateConsignmentOrderInsideShopAsync(
    int consignmentId,
    decimal consignmentPrice,
    string? consignmentTitle = null,
    string? consignmentDetail = null)
        {
            try
            {
                // Lấy UserId từ thông tin người dùng đã xác thực
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Người dùng chưa xác thực.");
                }

                // Gọi phương thức dịch vụ để cập nhật consignment và giá Koi
                var isUpdated = await _consignmentService.UpdateConsignmentOrderAsync(
                    consignmentId, userId, consignmentPrice, consignmentTitle, consignmentDetail);

                if (!isUpdated)
                {
                    return NotFound("Không tìm thấy đơn ký gửi hoặc không thể cập nhật.");
                }

                return Ok("Cập nhật đơn ký gửi thành công.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi server nội bộ: {ex.Message}");
            }
        }





        [Authorize(Roles = "customer")]
        [HttpPut("update-consignment-take-care-inside-shop/{consignmentId}")]
        public async Task<IActionResult> UpdateConsignmentTakeCareInsideShop(
    int consignmentId,
    int koitypeID,
    int koiID,
    DateTime consignmentDateTo,
    string? userImage = null,
    string? consignmentTitle = null,
    string? consignmentDetail = null
)
        {
            try
            {
                // Validate consignmentDateTo to ensure it's not in the past
                if (consignmentDateTo < DateTime.Now)
                {
                    return BadRequest("Consignment date cannot be in the past.");
                }

                // Get the UserId from the claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Call service to update consignment
                var updated = await _consignmentService.UpdateConsignmentTakeCareInsideShopAsync(
                    consignmentId, userId, koitypeID, koiID, consignmentDateTo, userImage, consignmentTitle, consignmentDetail
                );

                if (!updated)
                {
                    return NotFound("Consignment not found or not authorized to update.");
                }

                return Ok("Consignment updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [Authorize(Roles = "customer")]
        [HttpPut("update-consignment-take-care-outside-shop/{consignmentId}")]
        public async Task<IActionResult> UpdateConsignmentTakeCareOutsideShop(
    int consignmentId,
    
    string? name = null,
    string? origin = null,
    string? gender = null,
    int? age = null,
    decimal? size = null,
    string? breed = null,
    string? personality = null,
    decimal? feedingAmount = null,
    decimal? filterRate = null,
    string? healthStatus = null,
    string? awardCertificates = null,
    string? description = null,
    string? detailDescription = null,
    string? imageKoi = null,
    string? imageCertificate = null,
    string? additionImage = null,
    DateTime? consignmentDateTo = null,
    string? consignmentTitle = null,
    string? consignmentDetail = null)
        {
            try
            {
                // Get the UserId from the authenticated user's claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Call the service to update the consignment and koi details
                var isUpdated = await _consignmentService.UpdateConsignmentTakeCareOutsideShopAsync(
                    consignmentId, userId,  name, origin, gender, age, size, breed,
                    personality, feedingAmount, filterRate, healthStatus, awardCertificates, description,
                    detailDescription, imageKoi, imageCertificate, additionImage, consignmentDateTo,
                    consignmentTitle, consignmentDetail);

                if (!isUpdated)
                {
                    return NotFound("Consignment or associated Koi not found or could not be updated.");
                }

                return Ok("Consignment updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [Authorize(Roles = "customer")]
        [HttpPut("update-consignment-order-from-outside-shop/{consignmentId}")]
        public async Task<IActionResult> UpdateConsignmentOrderFromOutsideShopAsync(
    int consignmentId,
    decimal consignmentPrice,
    string? name = null,
    string? origin = null,
    string? gender = null,
    int? age = null,
    decimal? size = null,
    string? breed = null,
    string? personality = null,
    decimal? feedingAmount = null,
    decimal? filterRate = null,
    string? healthStatus = null,
    string? awardCertificates = null,
    string? description = null,
    string? detailDescription = null,
    string? imageKoi = null,
    string? imageCertificate = null,
    string? additionImage = null,
    string? consignmentTitle = null,
    string? consignmentDetail = null)
        {
            try
            {
                // Retrieve the UserId from the authenticated user's claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Call the service method to update both consignment and koi details
                var isUpdated = await _consignmentService.UpdateConsignmentOrderFromOutsideShopAsync(
                    consignmentId, userId, consignmentPrice, name, origin, gender, age, size, breed,
                    personality, feedingAmount, filterRate, healthStatus, awardCertificates, description,
                    detailDescription, imageKoi, imageCertificate, additionImage, consignmentTitle, consignmentDetail);

                if (!isUpdated)
                {
                    return NotFound("Consignment order not found or could not be updated.");
                }

                return Ok("Consignment order updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }





        [Authorize(Roles = "manager, staff")]
        // PUT: api/consignment/update-status-consignment-take-care-in-and-out
        [HttpPut("update-status-consignment-take-care-in-and-out")]
        public async Task<IActionResult> UpdateStatusConsignmentTakeCareInAndOut([FromBody] UpdateStatusRequest request)
        {
            try
            {
                // Check the current user from Claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated.");
                }

                // Verify role (only manager and staff are allowed)
                if (!HttpContext.User.IsInRole("manager") && !HttpContext.User.IsInRole("staff"))
                {
                    return Forbid("Only managers or staff can update the consignment status.");
                }

                // Retrieve consignment details to confirm existence and get user information
                var consignment = await _consignmentService.GetConsignmentByIdAsync(request.ConsignmentId);
                if (consignment == null)
                {
                    return NotFound("Consignment not found.");
                }

                // Call the service method to update the status
                var updated = await _consignmentService.UpdateConsignmentStatusAsync(request.ConsignmentId, request.Status);

                if (!updated)
                {
                    return NotFound("Failed to update the consignment.");
                }

                // Get user info to send an email notification
                var user = await _userService.GetUserByIdAsync(consignment.UserId.Value);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Send notification email
                    string subject = "Consignment Status Update";
                    string message = $"Dear {user.UserName},\n\nYour consignment with ID {request.ConsignmentId} has been updated to status '{request.Status}'.\n\nBest regards,\nKoi Farm Team";

                    await _emailService.SendEmailAsync(user.Email, subject, message);
                }

                return Ok("Consignment status updated successfully and user notified via email.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }



        [Authorize(Roles = "manager, staff")]
        [HttpPost("list-consignment-for-sale/{consignmentId}")]
        public async Task<IActionResult> ListConsignmentForSale(int consignmentId)
        {
            try
            {
                var consignment = await _consignmentService.GetConsignmentByIdAsync(consignmentId);
                if (consignment == null)
                {
                    return NotFound("Consignment not found.");
                }

                // Không cho phép đăng bán nếu loại consignment là 'offline'
                if (consignment.ConsignmentType == "offline")
                {
                    return BadRequest("Offline consignments cannot be listed for sale.");
                }
                // Chỉ cho phép các ký gửi đã được phê duyệt để đăng bán
                if (consignment.Status != "approved")
                {
                    return BadRequest("Only approved consignments can be listed for sale.");
                }

                // Cập nhật trạng thái ký gửi thành 'available'
                bool updated = await _consignmentService.UpdateConsignmentStatusAsync(consignmentId, "available");

                if (!updated)
                {
                    return NotFound("Failed to update the consignment.");
                }

                // Lấy thông tin người dùng để gửi email
                var user = await _userService.GetUserByIdAsync(consignment.UserId.Value);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Gửi email thông báo
                    string subject = "Your Consignment is Now Available for Sale";
                    string message = $"Dear {user.UserName},\n\nYour consignment with ID {consignmentId} has been approved and is now available for sale.\n\nBest regards,\nKoi Farm Team";

                    await _emailService.SendEmailAsync(user.Email, subject, message);
                }

                return Ok("Consignment is now listed for sale and the user has been notified.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }






        // DELETE: api/consignment/delete-consignment/{consignmentId}
        [HttpDelete("delete-consignment/{consignmentId}")]
        public async Task<IActionResult> DeleteConsignment(int consignmentId)
        {
            var result = await _consignmentService.DeleteConsignmentAsync(consignmentId);
            if (!result)
            {
                return NotFound("Consignment not found.");
            }
            return Ok("Consignment deleted successfully.");
        }
        // POST: api/consignment/notify-customer
        [HttpPost("notify-customer")]
        public async Task<IActionResult> NotifyCustomer(int consignmentId, string customerEmail)
        {
            try
            {
                string subject = "Consignment Status Update";
                string message = $"Dear Customer, \n\nYour consignment with ID {consignmentId} has been updated.\n\nBest regards,\nKoi Farm Team";

                await _emailService.SendEmailAsync(customerEmail, subject, message);

                return Ok("Email sent successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Failed to send email: {ex.Message}");
            }
        }


    }
}