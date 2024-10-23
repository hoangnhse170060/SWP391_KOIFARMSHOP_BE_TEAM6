using KMG.Repository.Base;
using KMG.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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



        public async Task<IEnumerable<Feedback>> GetAllAsync(Expression<Func<Feedback, bool>> predicate)
        {
            return await _context.Feedbacks.Where(predicate).ToListAsync();
        }

        public void RemoveRange(IEnumerable<Feedback> entities)
        {
            _context.Feedbacks.RemoveRange(entities);
        }
    }
}
