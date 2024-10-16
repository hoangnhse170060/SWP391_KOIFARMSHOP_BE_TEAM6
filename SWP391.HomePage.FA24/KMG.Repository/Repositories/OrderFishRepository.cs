using KMG.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Repositories
{
    public class OrderFishRepository
    {
        private readonly SwpkoiFarmShopContext _context;
        public OrderFishRepository(SwpkoiFarmShopContext context) => _context = context;
        public IQueryable<OrderFish> GetAll()
        {
            return _context.OrderFishes;
        }
    }
}
