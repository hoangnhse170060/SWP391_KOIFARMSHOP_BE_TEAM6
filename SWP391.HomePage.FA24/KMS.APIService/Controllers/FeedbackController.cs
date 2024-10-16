using KMG.Repository;
using Microsoft.EntityFrameworkCore;
using KMG.Repository.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KMG.Repository.Models;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;
        public FeedbackController(UnitOfWork unitOfWork) => _unitOfWork = unitOfWork;
        [HttpGet("getFeedbackbyKoiid/{koiID}")]
        public async Task<IActionResult> GetFeedbackByKoiId(int koiID)
        {
            var feedbacks = await _unitOfWork.FeedbackRepository.GetAll()
                .Where(f => f.KoiId == koiID)
                .Select(f => new
                {
                    FeedbackId = f.FeedbackId,
                    UserId = f.UserId,
                    Rating = f.Rating,
                    Content = f.Content,
                    FeedbackDate = f.FeedbackDate
                }).ToListAsync();

            if (feedbacks == null || !feedbacks.Any())
            {
                return NotFound("No feedback found for the given Koi or Fish.");
            }

            return Ok(feedbacks);
        }
        [HttpGet("getFeedbackbyFishid/{fishesID}")]
        public async Task<IActionResult> GetFeedbackByFishId(int fishesID)
        {
            var feedbacks = await _unitOfWork.FeedbackRepository.GetAll()
                .Where(f =>f.FishesId == fishesID)
                .Select(f => new
                {
                    FeedbackId = f.FeedbackId,
                    UserId = f.UserId,
                    Rating = f.Rating,
                    Content = f.Content,
                    FeedbackDate = f.FeedbackDate
                }).ToListAsync();

            if (feedbacks == null || !feedbacks.Any())
            {
                return NotFound("No feedback found for the given Koi or Fish.");
            }

            return Ok(feedbacks);
        }
        [HttpPost("add")]
        public async Task<IActionResult> AddFeedback(int userId, int orderId, int rating, string content, int? koiId = null, int? fishesId = null)
        {
           
            var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
            if (order == null || order.OrderStatus != "completed")
            {
                return BadRequest("Order not found or is not completed.");
            }

           
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            if (koiId != null)
            {
                var koiExistsInOrder = await _unitOfWork.OrderKoiRepository.GetAll()
                    .AnyAsync(ok => ok.OrderId == orderId && ok.KoiId == koiId);

                if (!koiExistsInOrder)
                {
                    return BadRequest("Koi not found in the order.");
                }
            }

            
            if (fishesId != null)
            {
                var fishesExistsInOrder = await _unitOfWork.OrderFishRepository.GetAll()
                    .AnyAsync(of => of.OrderId == orderId && of.FishesId == fishesId);

                if (!fishesExistsInOrder)
                {
                    return BadRequest("Fish not found in the order.");
                }
            }

            // Tạo đối tượng Feedback mới
            var feedback = new Feedback
            {
                UserId = userId,
                OrderId = orderId,
                KoiId = koiId, // Gán giá trị KoiId nếu có
                FishesId = fishesId, // Gán giá trị FishesId nếu có
                Rating = rating,
                Content = content,
                FeedbackDate = DateOnly.FromDateTime(DateTime.Now)
            };

            // Thêm Feedback vào cơ sở dữ liệu
            await _unitOfWork.FeedbackRepository.CreateAsync(feedback);
            await _unitOfWork.FeedbackRepository.SaveAsync(); // Lưu thay đổi vào DB

            return Ok("Feedback has been added successfully.");
        }

    }
}

