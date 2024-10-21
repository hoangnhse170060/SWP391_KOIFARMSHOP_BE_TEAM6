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
        public async Task<ActionResult<IEnumerable<Feedback>>>
            GetKoi()
        {
            var feedbackList = await _unitOfWork.FeedbackRepository .GetAllAsync();
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
      
        
        [HttpPost("add/{orderId}")]
        [Authorize(Roles = "customer")]
        public async Task<IActionResult> AddFeedback(int orderId, int rating, string content, int? koiId = null, int? fishesId = null)
        {   
            var userIdClaim = HttpContext.User.Claims.FirstOrDefault(c => c.Type == "Id");
            if (userIdClaim == null)
            {
                return Unauthorized("User is not authenticated.");
            }

            int userId = int.Parse(userIdClaim.Value); 

           
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


    }
}

