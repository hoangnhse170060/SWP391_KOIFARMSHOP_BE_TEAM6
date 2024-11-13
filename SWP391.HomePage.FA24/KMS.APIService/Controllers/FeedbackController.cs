using KMG.Repository;
using Microsoft.EntityFrameworkCore;
using KMG.Repository.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Authorization;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;
        public FeedbackController(UnitOfWork unitOfWork) => _unitOfWork = unitOfWork;
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetFeedback()
        {
            var feedbackList = await _unitOfWork.FeedbackRepository.GetAll().ToListAsync();
            Console.WriteLine($"Number of feedback retrieved: {feedbackList.Count}");
            return Ok(feedbackList);
        }

        [HttpDelete("delete/{feedbackId}")]
        public async Task<IActionResult> DeleteFeedback(int feedbackId)
        {

            var feedback = await _unitOfWork.FeedbackRepository.GetByIdAsync(feedbackId);

            if (feedback == null)
            {
                return NotFound("Feedback not found.");
            }
            await _unitOfWork.FeedbackRepository.RemoveAsync(feedback);
            await _unitOfWork.FeedbackRepository.SaveAsync();

            return Ok("Feedback deleted successfully.");
        }

        [HttpGet("getfeedbackbykoiid/{koiId}")]
        public async Task<IActionResult> GetFeedbackByKoiId(int koiId)
        {
            var feedbacks = await _unitOfWork.FeedbackRepository.GetFeedbackWithKoiName(koiId);

            if (!feedbacks.Any())
            {
                return NotFound("No feedback found for the given Koi ID.");
            }

            return Ok(feedbacks);
        }

        [HttpGet("getfeedbackbyfishid/{fishId}")]
        public async Task<IActionResult> GetFeedbackByFishId(int fishId)
        {
            var feedbacks = await _unitOfWork.FeedbackRepository.GetFeedbackWithFishName(fishId);

            if (!feedbacks.Any())
            {
                return NotFound("No feedback found for the given Fish ID.");
            }

            return Ok(feedbacks);
        }



        [HttpPost("add/{orderId}")]
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> AddFeedback(int orderId, int rating, string content, int? koiId = null, int? fishesId = null)
        {
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "UserId");
            if (userIdClaim == null)
            {
                return Unauthorized("User is not authenticated.");
            }

            int userId = int.Parse(userIdClaim.Value);

            var isOwner = await _unitOfWork.OrderRepository.IsUserOrderOwnerAsync(userId, orderId);
            if (!isOwner)
            {
                return BadRequest("You did not purchase this order, so you cannot leave feedback.");
            }
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
            var existingFeedback = await _unitOfWork.FeedbackRepository.GetAllL()
            .AnyAsync(f => f.UserId == userId && f.OrderId == orderId &&
                       f.KoiId == koiId && f.FishesId == fishesId);
            if (existingFeedback)
            {
                return BadRequest("You have already provided feedback for this order item.");
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
                var fishesExistsInOrder = await _unitOfWork.OrderFishesRepository.GetAll()
                    .AnyAsync(of => of.OrderId == orderId && of.FishesId == fishesId);

                if (!fishesExistsInOrder)
                {
                    return BadRequest("Fish not found in the order.");
                }
            }


            var feedback = new Feedback
            {
                UserId = userId,
                OrderId = orderId,
                KoiId = koiId,
                FishesId = fishesId,
                Rating = rating,
                Content = content,
                FeedbackDate = DateOnly.FromDateTime(DateTime.Now)
            };


            await _unitOfWork.FeedbackRepository.CreateAsync(feedback);
            await _unitOfWork.FeedbackRepository.SaveAsync();

            return Ok("Feedback has been added successfully.");
        }
        [HttpGet("average-rating-fish/{fishId}")]
        public async Task<IActionResult> GetAverageRatingForFish(int fishId)
        {
            var averageRating = await _unitOfWork.FeedbackRepository.GetAverageRatingForFish(fishId);

            return Ok(new { FishId = fishId, AverageRating = averageRating });
        }

        [HttpGet("average-rating-koi/{koiId}")]
        public async Task<IActionResult> GetAverageRatingForKoi(int koiId)
        {
            var averageRating = await _unitOfWork.FeedbackRepository.GetAverageRatingForKoi(koiId);

            return Ok(new { KoiId = koiId, AverageRating = averageRating });
        }


    }
}

