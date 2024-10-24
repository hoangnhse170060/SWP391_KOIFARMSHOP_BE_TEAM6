using KMG.Repository.Base;
using KMG.Repository.Models;
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
    }
}
