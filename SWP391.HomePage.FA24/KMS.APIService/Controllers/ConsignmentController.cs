using AutoMapper;
using KMG.Repository.Dto;
using KMG.Repository.Interfaces;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Crypto;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin, staff, customer")]
    public class ConsignmentController : ControllerBase
    {
        private readonly IConsignmentService _consignmentService;
        private readonly IMapper _mapper;  // Inject IMapper

        public ConsignmentController(IConsignmentService consignmentService, IMapper mapper)
        {
            _consignmentService = consignmentService;
            _mapper = mapper;  // Assign IMapper to the private field
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

        [Authorize(Roles = "customer")]
        // POST: api/consignment/create-consignment
        [HttpPost("create-consignmentCustomer")]
        public async Task<IActionResult> CreateConsignment(
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

                // Create consignment using the service
                var createdConsignment = await _consignmentService.CreateConsignmentAsync(
                    userId, koiID, consignmentType, status, consignmentPrice, DateTime.Now, consignmentDateTo, userImage, consignmentTitle, consignmentDetail
                );

                // Return created consignment with CreatedAtAction
                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = createdConsignment.ConsignmentId }, createdConsignment);
            }
            catch (Exception ex)
            {
                // Log exception and return server error
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [Authorize(Roles = "admin, staff")]
        [HttpPost("create-consignmentAdmin_Staff")]
        public async Task<IActionResult> CreateConsignment(
                int koiID,
                string consignmentType,
                decimal consignmentPrice,
                //DateTime consignmentDateFrom,
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

                //var dateOnly = DateOnly.FromDateTime(consignmentDate);

                // Create consignment using the service
                var createdConsignment = await _consignmentService.CreateConsignmentAsync(
                    userId, koiID, consignmentType, status, consignmentPrice, DateTime.Now, consignmentDateTo, userImage, consignmentTitle, consignmentDetail
                );

                // Return created consignment
                return CreatedAtAction(nameof(GetConsignmentById), new { consignmentId = createdConsignment.ConsignmentId }, createdConsignment);
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
                    consignmentId, userId, koiID, consignmentType, status, consignmentPrice, DateTime.Now, consignmentDateTo, userImage, consignmentTitle, consignmentDetail
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


        [Authorize(Roles = "admin, staff")]
        // PUT: api/consignment/update-consignment/{consignmentId}
        [HttpPut("update-consignmentByAdmin_Staff/{consignmentId}")]
        public async Task<IActionResult> UpdateConsignment(
                int consignmentId,
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
                    consignmentId, userId, koiID, consignmentType, status, consignmentPrice, DateTime.Now, consignmentDateTo, userImage, consignmentTitle, consignmentDetail
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
