using KMG.Repository.Models;
using KMG.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderFishesController : ControllerBase
    {

        private readonly UnitOfWork _unitOfWork;
        private readonly ILogger<OrderFishesController> _logger;

        public OrderFishesController(UnitOfWork unitOfWork, ILogger<OrderFishesController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderFish>>> GetOrders()
        {
            var OrderFish = await _unitOfWork.OrderFishesRepository.GetAllAsync();
            return Ok(OrderFish);
        }

        [HttpPost]
        public async Task<IActionResult> AddFishesToOrder(OrderFish orderFish)
        {
            // Kiểm tra xem cặp orderID và koiID đã tồn tại chưa
            var orderkoi = await _unitOfWork.OrderFishesRepository
                 .FirstOrDefaultAsync(ok => ok.OrderId == orderFish.OrderId && ok.FishesId == orderFish.FishesId);

            if (orderkoi != null)
            {
                return Conflict("This order already contains the specified koi.");
            }

            // Nếu không tồn tại, thêm bản ghi mới
            await _unitOfWork.OrderFishesRepository.CreateAsync(orderFish);
            return Ok(orderFish);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrderFishes(int id, [FromBody] OrderFish orderFish)
        {
            if (id != orderFish.OrderId)
            {
                return BadRequest("Order ID mismatch.");
            }
            try
            {
                await _unitOfWork.OrderFishesRepository.UpdateAsync(orderFish);
                await _unitOfWork.OrderFishesRepository.SaveAsync();
                return Ok("Order_Fishes has been successfully updated.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound("The Order_Fishes does not exist.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the order");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{orderId}/{FishesId}")]
        public async Task<IActionResult> DeleteKoiFromOrder(int orderId, int FishesId)
        {
            // Tìm bản ghi cần xóa
            var orderFish = await _unitOfWork.OrderFishesRepository
                .FirstOrDefaultAsync(ok => ok.OrderId == orderId && ok.FishesId == FishesId);

            if (orderFish == null)
            {
                // Nếu không tìm thấy, trả về 404 Not Found
                return NotFound("Order_Fishes not found.");
            }

            // Gọi phương thức xóa
            _unitOfWork.OrderFishesRepository.Remove(orderFish);

            // Lưu thay đổi vào cơ sở dữ liệu
            await _unitOfWork.OrderFishesRepository.SaveAsync();

            // Trả về 204 No Content để xác nhận xóa thành công
            return NoContent();
        }

    }
}
