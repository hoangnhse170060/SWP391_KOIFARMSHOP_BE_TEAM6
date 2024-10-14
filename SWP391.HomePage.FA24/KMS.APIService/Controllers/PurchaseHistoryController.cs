using KMG.Repository.Models;
using KMG.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PurchaseHistoryController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;
        public PurchaseHistoryController(UnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseHistory>>>
            GetHistory()
        {
            var historyList = await _unitOfWork.PurchaseHistoryRepository.GetAllAsync();
            Console.WriteLine($"Number of History retrieved: {historyList.Count}");
            return Ok(historyList);

        }
        [HttpGet("getPurchaseHistoryByUserID/{userID}")]
        public async Task<IActionResult> GetPurchaseHistoryByUserID(int userID)
        {
           
            var purchaseHistory = await _unitOfWork.PurchaseHistoryRepository.GetAll()
                .Where(p => p.UserId == userID)
                .Select(p => new
                {
                    p.OrderId,
                    p.PurchaseDate,
                    p.TotalMoney,
                    p.DiscountMoney,
                    p.FinalMoney,
                    p.OrderStatus,
                    p.PaymentMethod,
                    p.ShippingDate,
                    p.DeliveryStatus,
                    p.PromotionId,
                    p.EarnedPoints,
                    p.UsedPoints
                })
                .ToListAsync();

            
            if (purchaseHistory == null || !purchaseHistory.Any())
            {
                return NotFound("No purchase history found for the given user ID.");
            }

            return Ok(purchaseHistory);
        }

    }
}
