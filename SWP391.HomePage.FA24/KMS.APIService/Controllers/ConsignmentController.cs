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
        private readonly IMapper _mapper;
        private readonly SwpkoiFarmShopContext _context;
        private readonly IEmailService _emailService;
        private readonly IUserService _userService;

        public ConsignmentController(IConsignmentService consignmentService, IMapper mapper, SwpkoiFarmShopContext context, IEmailService emailService, IUserService userService)
        {
            _consignmentService = consignmentService;
            _mapper = mapper;
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
    int koitypeID, int koiID, string consignmentType, DateTime consignmentDateTo, string? userImage = null, string? consignmentTitle = null, string? consignmentDetail = null)
        {
            try
            {
                if (consignmentDateTo < DateTime.Now)
                {
                    return BadRequest("Consignment date cannot be in the past.");
                }

                // Check if consignmentDateTo is at least 7 days from now
                if (consignmentDateTo < DateTime.Now.AddDays(7))
                {
                    return BadRequest("Consignment date must be at least 7 days from today.");
                }

                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var status = "awaiting inspection";

                // Gọi phương thức CreateConsignmentAsync để tạo ký gửi và tính phí chăm sóc
                var createdConsignment = await _consignmentService.CreateConsignmentAsync(
                    userId, koitypeID, koiID, consignmentType, status, 0, DateTime.Now, consignmentDateTo, userImage, consignmentTitle, consignmentDetail);

                // Trả về consignment kèm theo phí chăm sóc
                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = createdConsignment.ConsignmentId }, new
                {
                    consignment = createdConsignment,
                    takeCareFee = createdConsignment.TakeCareFee
                });
            }
            catch (Exception ex)
            {
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
                if (consignmentPrice <= 0)
                {
                    return BadRequest("Consignment price must be greater than 0.");
                }
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
        public async Task<IActionResult> CreateConsignmentOrderFromOutsideShop([FromBody] ConsignmentOrderRequestDto request)
        {
            try
            {
                if (request.ConsignmentPrice <= 0)
                {
                    return BadRequest("Consignment price must be greater than 0.");
                }
                // Retrieve the UserId from the authenticated user's claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Call the service method to create a consignment order
                var createdConsignment = await _consignmentService.CreateConsignmentOrderFromOutsideShopAsync(userId, request);

                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = createdConsignment.ConsignmentId }, createdConsignment);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [Authorize(Roles = "customer")]
        [HttpPost("create-consignment-take-care-outside-shop")]
        public async Task<IActionResult> CreateConsignmentTakeCareOutsideShop([FromBody] ConsignmentTakeCareOutsideRequestDto request)
        {
            try
            {
                // Validate consignment date: cannot be in the past
                if (request.ConsignmentDateTo < DateTime.Now)
                {
                    return BadRequest("Consignment date cannot be in the past.");
                }

                // Validate consignment date: must be at least 7 days from today
                if (request.ConsignmentDateTo < DateTime.Now.AddDays(7))
                {
                    return BadRequest("Consignment date must be at least 7 days from today.");
                }
                // Get the UserId from the claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Call service to create consignment and koi record
                var consignmentDto = await _consignmentService.CreateConsignmentTakeCareOutsideShopAsync(userId, request);

                // Return the created consignment
                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = consignmentDto.ConsignmentId }, consignmentDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize(Roles = "manager, staff, customer")]
        [HttpPut("update-consignment-title-detail/{consignmentId}")]
        public async Task<IActionResult> UpdateConsignmentTitleAndDetail(
   int consignmentId,
   [FromBody] UpdateConsignmentTitleDetailRequestDto request)
        {
            try
            {
                // Get the UserId from the claims
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Call service to update consignment title and detail
                var updated = await _consignmentService.UpdateConsignmentTitleAndDetailAsync(
                    consignmentId, userId, request.ConsignmentTitle, request.ConsignmentDetail
                );

                if (!updated)
                {
                    return NotFound("Consignment not found or not authorized to update.");
                }

                return Ok("Consignment title and detail updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize(Roles = "manager, staff, customer")]
        [HttpPut("update-order-consignment/{consignmentId}")]
        public async Task<IActionResult> UpdateOrderConsignmentAsync(
            int consignmentId,
            [FromBody] UpdateOrderConsignmentRequestDto request)
        {
            try
            {
                // Lấy UserId từ claim để kiểm tra người dùng
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                // Gọi phương thức dịch vụ để cập nhật các trường cần thiết
                var isUpdated = await _consignmentService.UpdateConsignmentOrderFieldsAsync(consignmentId, userId, request);

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
        [HttpPut("update-consignment-order-status")]
        public async Task<IActionResult> UpdateConsignmentOrderStatus([FromBody] UpdateStatusRequest request)
        {
            try
            {
                // Get the UserId from the claims to ensure the user is authenticated
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated.");
                }

                // Only allow manager or staff roles to update status
                if (!HttpContext.User.IsInRole("manager") && !HttpContext.User.IsInRole("staff"))
                {
                    return Forbid("Only managers or staff can update the consignment status.");
                }

                // Call the service method to update the consignment and koi status
                var isUpdated = await _consignmentService.UpdateConsignmentOrderStatusAsync(request.ConsignmentId, request.Status);

                if (!isUpdated)
                {
                    return NotFound("Consignment order not found or could not be updated.");
                }

                // Get consignment details to fetch the user information
                var consignment = await _consignmentService.GetConsignmentByIdAsync(request.ConsignmentId);
                if (consignment == null || !consignment.UserId.HasValue)
                {
                    return NotFound("Consignment details not found.");
                }

                // Get the user details associated with the consignment
                var user = await _userService.GetUserByIdAsync(consignment.UserId.Value);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    // Prepare the email notification
                    string subject = "Update on Your Consignment Order Status";
                    string message = $"Dear {user.UserName},\n\n" +
                                     $"The status of your consignment with ID {request.ConsignmentId} has been updated to '{request.Status}'.\n\n" +
                                     $"If you have any questions, please contact our support team.\n\n" +
                                     "Best regards,\nKoi Farm Team";

                    // Send the email
                    await _emailService.SendEmailAsync(user.Email, subject, message);
                }

                return Ok("Consignment order status updated successfully and the user has been notified.");
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
        

    }
}