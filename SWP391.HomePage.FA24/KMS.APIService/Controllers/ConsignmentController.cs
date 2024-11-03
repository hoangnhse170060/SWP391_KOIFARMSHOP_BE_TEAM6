using AutoMapper;
using KMG.Repository.Dto;
using KMG.Repository.Interfaces;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        public ConsignmentController(IConsignmentService consignmentService, IMapper mapper, SwpkoiFarmShopContext context)
        {
            _consignmentService = consignmentService;
            _mapper = mapper;  // Assign IMapper to the private field
            _context = context;
        }

        // GET: api/consignment/get-consignments
        [HttpGet("get-consignments")]
        public async Task<IActionResult> GetAllConsignments()
        {
            var consignments = await _consignmentService.GetAllConsignmentsAsync();
            var consignmentsDto = _mapper.Map<IEnumerable<ConsignmentDto>>(consignments);
            return Ok(consignmentsDto);  // Return mapped DTOs
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

        // GET: api/consignment/get-consignments-by-user/{userId}
        [HttpGet("get-consignments-by-user/{userId}")]
        public async Task<IActionResult> GetConsignmentsByUserId(int userId)
        {
            try
            {
                var consignments = await _consignmentService.GetConsignmentsByUserIdAsync(userId);
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
        [HttpPost("create-consignmentCustomer")]
        public async Task<IActionResult> CreateConsignment(
         int koitypeID,
         int koiID,
         string consignmentType,
         decimal consignmentPrice,
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

                // Calculate the consignment fee based on the duration
                var currentDate = DateTime.Now;
                var totalMonths = ((consignmentDateTo.Year - currentDate.Year) * 12) + consignmentDateTo.Month - currentDate.Month;

                if (consignmentDateTo.Day > currentDate.Day)
                {
                    totalMonths++; // Nếu ngày ký gửi lớn hơn ngày hiện tại, cộng thêm 1 tháng
                }

                // Ensure at least one month fee
                totalMonths = Math.Max(totalMonths, 1);

                decimal consignmentFee = totalMonths * 100000; // Fee per month is 100,000 VND

                // Create consignment using the service
                var createdConsignment = await _consignmentService.CreateConsignmentAsync(
                    userId, koitypeID, koiID, consignmentType, status, consignmentPrice, DateTime.Now, consignmentDateTo, userImage, consignmentTitle, consignmentDetail
                );

                // Return created consignment with CreatedAtAction
                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = createdConsignment.ConsignmentId }, new
                {
                    consignment = createdConsignment,
                    consignmentFee // Include the calculated consignment fee in the response
                });
            }
            catch (Exception ex)
            {
                // Log exception and return server error
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }



        [Authorize(Roles = "manager, staff")]
        [HttpPost("create-consignmentAdmin_Staff")]
        public async Task<IActionResult> CreateConsignment(
        int koitypeID,
        int koiID,
        string consignmentType,
        decimal consignmentPrice,
        DateTime consignmentDateTo,
        string? userImage = null,
        string? consignmentTitle = null,
        string? consignmentDetail = null,
        string? status = null) // Optional status parameter
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

                // Tính toán consignmentFee dựa trên số tháng từ DateTime.Now tới consignmentDateTo
                var months = ((consignmentDateTo.Year - DateTime.Now.Year) * 12) + consignmentDateTo.Month - DateTime.Now.Month;
                var consignmentFee = months * 100000; // Mỗi tháng 100,000 VND

                // Tạo consignment sử dụng service
                var createdConsignment = await _consignmentService.CreateConsignmentAsync(
                    userId, koitypeID, koiID, consignmentType, status, consignmentPrice, DateTime.Now, consignmentDateTo, userImage, consignmentTitle, consignmentDetail
                );

                // Trả về consignment đã tạo cùng với consignmentFee
                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = createdConsignment.ConsignmentId }, new
                {
                    consignment = createdConsignment,
                    consignmentFee // Bao gồm phí ký gửi trong phản hồi
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        


        [Authorize(Roles = "customer")]
        // PUT: api/consignment/update-consignment/{consignmentId}
        [HttpPut("update-consignmentCustomer/{consignmentId}")]
        public async Task<IActionResult> UpdateConsignment(
        int consignmentId,
        int koitypeID,
        int koiID,
        string consignmentType,
        decimal consignmentPrice,
        // DateTime consignmentDateFrom,
        DateTime consignmentDateTo,
        string? userImage,
        string? consignmentTitle = null,
        string? consignmentDetail = null)
        {
            try
            {
                // Validate consignmentDateTo to ensure it's not in the past
                if (consignmentDateTo < DateTime.Now)
                {
                    return BadRequest("Consignment date cannot be in the past.");
                }
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated.");
                }

                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    return BadRequest("Invalid User ID.");
                }

                //var dateOnly = DateOnly.FromDateTime(consignmentDate);

                var status = "awaiting inspection";


                // Update consignment using the service
                var updated = await _consignmentService.UpdateConsignmentAsync(
                    consignmentId, userId, koitypeID, koiID, consignmentType, status, consignmentPrice, DateTime.Now, consignmentDateTo, userImage, consignmentTitle, consignmentDetail
                );

                if (!updated)
                {
                    return NotFound("Consignment not found.");
                }

                return Ok("Consignment updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }


        [Authorize(Roles = "manager, staff")]
        // PUT: api/consignment/update-consignment/{consignmentId}
        [HttpPut("update-consignmentByAdmin_Staff/{consignmentId}")]
        public async Task<IActionResult> UpdateConsignment(
                int consignmentId,
                int koitypeID,
                int koiID,
                string consignmentType,
                decimal consignmentPrice,
                //DateTime consignmentDateFrom,
                DateTime consignmentDateTo,
                string? userImage,
                string? consignmentTitle = null,
                string? consignmentDetail = null,
                string? status = null) // Optional status parameter
        {
            try
            {
                // Validate consignmentDateTo to ensure it's not in the past
                if (consignmentDateTo < DateTime.Now)
                {
                    return BadRequest("Consignment date cannot be in the past.");
                }
                var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
                if (userIdClaim == null)
                {
                    return Unauthorized("User not authenticated.");
                }

                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    return BadRequest("Invalid User ID.");
                }

                //var dateOnly = DateOnly.FromDateTime(consignmentDate);

                // Determine user role
                var userRole = HttpContext.User.IsInRole("customer") ? "customer" : "staff/admin";

                if (userRole == "staff/admin")
                {
                    // Optional: Validate the status if provided for admin/staff
                    if (string.IsNullOrWhiteSpace(status))
                    {
                        return BadRequest("Status must be provided for admin or staff.");
                    }
                }
                else
                {
                    return BadRequest("Invalid user role.");
                }

                // Update consignment using the service
                var updated = await _consignmentService.UpdateConsignmentAsync(
                    consignmentId, userId, koitypeID, koiID, consignmentType, status, consignmentPrice, DateTime.Now, consignmentDateTo, userImage, consignmentTitle, consignmentDetail
                );

                if (!updated)
                {
                    return NotFound("Consignment not found.");
                }

                return Ok("Consignment updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
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