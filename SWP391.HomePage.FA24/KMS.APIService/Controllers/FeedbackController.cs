using KMG.Repository;
using Microsoft.EntityFrameworkCore;
using KMG.Repository.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

    }
}
