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
        public async Task<ActionResult<IEnumerable<object>>> GetHistoryWithDetails()
        {
            var historyList = await _unitOfWork.PurchaseHistoryRepository.GetAllWithDetails().ToListAsync();

            if (historyList == null || !historyList.Any())
            {
                return NotFound("No purchase history found.");
            }

            return Ok(historyList);
        }
        [HttpGet("getPurchaseHistoryByUserID/{userID}")]
        public async Task<IActionResult> GetPurchaseHistoryByUserID(int userID)
        {
            var purchaseHistory = await _unitOfWork.PurchaseHistoryRepository.GetAll()
                .Where(p => p.UserId == userID)
                .Select(p => new
                {
                    UserName = p.User.UserName,
                    p.OrderId,
                    p.PurchaseDate,
                    p.TotalMoney,
                    p.DiscountMoney,
                    p.FinalMoney,
                    p.OrderStatus,
                    p.PaymentMethod,
                    p.ShippingDate,

                    p.PromotionId,
                    p.EarnedPoints,
                    p.UsedPoints,
                    KoiDetails = p.Order.OrderKois.Select(ok => new
                    {
                        ok.KoiId,
                        ok.Koi.Name,
                        ok.Koi.ImageKoi,
                        ok.Quantity
                    }).ToList(),
                    FishDetails = p.Order.OrderFishes.Select(of => new
                    {
                        of.FishesId,
                        of.Fishes.Name,
                        of.Fishes.ImageFishes,
                        of.Quantity
                    }).ToList()
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
