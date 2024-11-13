using KMG.Repository.Models;
using KMG.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using KMG.Repository.Repositories;
using Microsoft.Extensions.Logging;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderKoiController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly ILogger<OrderController> _logger;

        public OrderKoiController(UnitOfWork unitOfWork, ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
        
            _logger = logger;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderKoi>>> GetOrders()
        {
            var OrderKoi = await _unitOfWork.OrderKoiRepository.GetAllAsync();
            return Ok(OrderKoi);
        }
        [HttpPost]
        public async Task<IActionResult> AddKoiToOrder(OrderKoi orderKoi)
        {
            // Kiểm tra xem cặp orderID và koiID đã tồn tại chưa
            var orderkoi = await _unitOfWork.OrderKoiRepository
                 .FirstOrDefaultAsync(ok => ok.OrderId == orderKoi.OrderId && ok.KoiId == orderKoi.KoiId);

            if (orderkoi != null)
            {
                return Conflict("This order already contains the specified koi.");
            }

            // Nếu không tồn tại, thêm bản ghi mới
            await _unitOfWork.OrderKoiRepository.CreateAsync(orderKoi);
            return Ok(orderKoi);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderKoi(int id, [FromBody] OrderKoi orderKoi)
        {
            if (id != orderKoi.OrderId)
            {
                return BadRequest("Order ID mismatch.");
            }
            try
            {
                await _unitOfWork.OrderKoiRepository.UpdateAsync(orderKoi);
                await _unitOfWork.OrderKoiRepository.SaveAsync();
                return Ok("Order_Koi has been successfully updated.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound("The Order_Koi does not exist.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the Order_Koi");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{orderId}/{koiId}")]
        public async Task<IActionResult> DeleteKoiFromOrder(int orderId)
        {
            // Tìm bản ghi cần xóa
            var order = await _unitOfWork.OrderRepository
                .FirstOrDefaultAsync(ok => ok.OrderId == orderId );

            if (order == null)
            {
                // Nếu không tìm thấy, trả về 404 Not Found
                return NotFound("Order_Koi not found.");
            }

            // Gọi phương thức xóa
            _unitOfWork.OrderRepository.Remove(order);

            // Lưu thay đổi vào cơ sở dữ liệu
            await _unitOfWork.OrderKoiRepository.SaveAsync();

            // Trả về 204 No Content để xác nhận xóa thành công
            return NoContent();
        }

    }
}
