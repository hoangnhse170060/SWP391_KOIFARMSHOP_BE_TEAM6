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

            return CreatedAtAction(nameof(GetHashCode), new { orderId = orderKoi.OrderId, koiId = orderKoi.KoiId }, orderKoi);
        }

    }
}
