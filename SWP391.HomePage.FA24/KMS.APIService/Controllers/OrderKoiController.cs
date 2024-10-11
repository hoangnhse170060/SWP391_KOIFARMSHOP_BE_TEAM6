using KMG.Repository.Models;
using KMG.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderKoiController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;

        public OrderKoiController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // 1. Thêm cá Koi vào đơn hàng
        [HttpPost]
        public async Task<ActionResult<OrderKoi>> AddKoiToOrder([FromBody] OrderKoi orderKoi)
        {
            if (orderKoi == null)
            {
                return BadRequest("OrderKoi object is null.");
            }

            // Kiểm tra xem OrderId và KoiId có hợp lệ không
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderKoi.OrderId);
            var koi = await _unitOfWork.KoiRepository.GetByIdAsync(orderKoi.KoiId);
            if (order == null || koi == null)
            {
                return NotFound("Order or Koi not found.");
            }

            // Thêm OrderKoi vào cơ sở dữ liệu
            await _unitOfWork.OrderKoiRepository.CreateAsync(orderKoi);
            await _unitOfWork.OrderKoiRepository.SaveAsync();

            return CreatedAtAction(nameof(GetOrderKoiById), new { orderId = orderKoi.OrderId, koiId = orderKoi.KoiId }, orderKoi);
        }

        // 2. Lấy danh sách tất cả OrderKoi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderKoi>>> GetAllOrderKoi()
        {
            var orderKoiList = await _unitOfWork.OrderKoiRepository.GetAllAsync();
            return Ok(orderKoiList);
        }

        // 3. Lấy OrderKoi theo ID
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderKoi>> GetOrderKoiById(int id)
        {
            var orderKoi = await _unitOfWork.OrderKoiRepository.GetByIdAsync(id);
            if (orderKoi == null)
            {
                return NotFound("OrderKoi not found.");
            }

            return Ok(orderKoi);
        }

        // 4. Xóa OrderKoi
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrderKoi(int id)
        {
            var orderKoi = await _unitOfWork.OrderKoiRepository.GetByIdAsync(id);
            if (orderKoi == null)
            {
                return NotFound("OrderKoi not found.");
            }

            await _unitOfWork.OrderKoiRepository.RemoveAsync(orderKoi);
            await _unitOfWork.OrderKoiRepository.SaveAsync();

            return NoContent();
        }
    }
}
