using KMG.Repository.Base;
using KMG.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Repositories
{
    public class OrderFishesRepository : GenericRepository<OrderFish>
    {

            private readonly SwpkoiFarmShopContext _context;
            public OrderFishesRepository(SwpkoiFarmShopContext context) => _context = context;

            public async Task<OrderFish> FirstOrDefaultAsync(Expression<Func<OrderFish, bool>> predicate)
            {
                return await _context.OrderFishes.FirstOrDefaultAsync(predicate);
            }


        public void Delete(OrderFish entity)
        {
            _context.OrderFishes.Remove(entity);
        }

        public async Task<IEnumerable<OrderFish>> GetByOrderIdAsync(int orderId)
        {
            return await _context.OrderFishes
                .Where(of => of.OrderId == orderId)
                .ToListAsync();
        }

        public void RemoveRange(IEnumerable<OrderFish> entities)
        {
            _context.OrderFishes.RemoveRange(entities);


        }


        public async Task<IEnumerable<Order>> GetAllAsync(
    Func<IQueryable<Order>, IIncludableQueryable<Order, object>> include = null)
        {
            var query = _context.Orders.AsQueryable();
            if (include != null)
            {
                query = include(query);
            }
            return await query.ToListAsync();
        }
        public IQueryable<OrderFish> GetAll()
        {
            return _context.OrderFishes;
        }


    }
}

