using KMG.Repository.Base;
using KMG.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Repositories
{
    public class FeedbackRepository : GenericRepository<Feedback>
    {
        private readonly SwpkoiFarmShopContext _context;
        public FeedbackRepository(SwpkoiFarmShopContext context) => _context = context;
        public IQueryable<Feedback> GetAll()
        {
            return _context.Feedbacks;

        }
        public async Task<IEnumerable<object>> GetFeedbackWithKoiName(int koiId)
        {
            return await _context.Feedbacks
                .Where(f => f.KoiId == koiId)
                .Join(
                    _context.Kois, 
                    feedback => feedback.KoiId,
                    koi => koi.KoiId,
                    (feedback, koi) => new
                    {
                        feedback.FeedbackId,
                        KoiName = koi.Name,
                        feedback.User.UserName,
                        feedback.Rating,
                        feedback.Content,
                        feedback.FeedbackDate,
                    }
                )
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetFeedbackWithFishName(int fishId)
        {
            return await _context.Feedbacks
                .Where(f => f.FishesId == fishId)
                .Join(
                    _context.Fishes, 
                    feedback => feedback.FishesId,
                    fish => fish.FishesId,
                    (feedback, fish) => new
                    {
                        feedback.FeedbackId,
                        FishName = fish.Name,
                        feedback.User.UserName,
                        feedback.Rating,
                        feedback.Content,
                        feedback.FeedbackDate,
                       
                    }
                )
                .ToListAsync();
        }
    }
}
