using KMG.Repository.Models;
using KMG.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KMG.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly ILogger<OrderController> _logger;

        public OrderController(UnitOfWork unitOfWork, ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var orders = await _unitOfWork.OrderRepository.GetAllAsync();
            return Ok(orders);
        }


        [HttpPost]
        public async Task<ActionResult<Koi>> CreateOrder([FromBody] Order order)
        {
            // Kiểm tra xem đối tượng koi có null không
            if (order == null)
            {
                return BadRequest("Koi object is null");
            }

            //// Kiểm tra các thuộc tính bắt buộc
            //if (string.IsNullOrEmpty(koi.Origin) || koi.KoiTypeId == null || koi.Age == null || koi.Size == null)
            //{
            //    return BadRequest("Missing required fields.");
            //}

            try
            {
                //var o = await _unitOfWork.OrderRepository.GetByIdAsync(order.OrderId);
                //if (koiType == null)
                //{
                //    return NotFound("Order not found.");
                //}
                //koi.Name = koiType.Name;
                await _unitOfWork.OrderRepository.CreateAsync(order);
                await _unitOfWork.OrderRepository.SaveAsync();


                return CreatedAtAction(nameof(GetOrders), new { id = order.OrderId }, order);
            }
            catch (DbUpdateException dbEx)
            {

                var innerException = dbEx.InnerException != null ? dbEx.InnerException.Message : "No inner exception";
                return StatusCode(500, $"Database update error: {dbEx.Message}, Inner Exception: {innerException}");
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order order)
        {
            if (id != order.OrderId)
            {
                return BadRequest("Order ID mismatch.");
            }

            try
            {
                await _unitOfWork.OrderRepository.UpdateAsync(order);
                await _unitOfWork.OrderRepository.SaveAsync();

                return Ok("Order has been successfully updated.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound("The order does not exist.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the order");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var result = await _unitOfWork.OrderRepository.DeleteWithId(id);
                if (result)
                {
                    return Ok(new { message = "Order deleted successfully." });
                }
                return NotFound(new { message = "Order not found." });
            }
            catch (DbUpdateException dbEx)
            {
                Console.WriteLine($"Ngoại lệ cập nhật cơ sở dữ liệu: {dbEx.Message}");
                if (dbEx.InnerException != null)
                {
                    Console.WriteLine($"Ngoại lệ bên trong: {dbEx.InnerException.Message}");
                }

                return BadRequest(new { message = "Có vấn đề khi xóa đơn hàng do ràng buộc cơ sở dữ liệu." });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ngoại lệ: {ex.Message}");
                return BadRequest(new { message = "Đã xảy ra lỗi khi xóa đơn hàng." });
            }

        }

    }
}
