using KMG.Repository.Base;
using KMG.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace KMG.Repository.Repositories
{
    public class OrderKoiRepository : GenericRepository<OrderKoi>
    {
        private readonly SwpkoiFarmShopContext _context;

        public OrderKoiRepository(SwpkoiFarmShopContext context) : base(context)
        {
            _context = context;
        }

        // Phương thức tìm kiếm với biểu thức điều kiện
        public async Task<OrderKoi> FirstOrDefaultAsync(Expression<Func<OrderKoi, bool>> predicate)
        {
            return await _context.OrderKois.FirstOrDefaultAsync(predicate);
        }

        // Phương thức lấy danh sách các OrderKoi theo OrderId
        public async Task<List<OrderKoi>> GetOrderKoisByOrderIdAsync(int orderId)
        {
            return await _context.OrderKois
                .Where(ok => ok.OrderId == orderId)
                .ToListAsync();
        }

        public void Delete(OrderKoi entity)
        {
            _context.OrderKois.Remove(entity);
        }

        public async Task<IEnumerable<OrderKoi>> GetByOrderIdAsync(int orderId)
        {
            return await _context.OrderKois
                .Where(ok => ok.OrderId == orderId)
                .ToListAsync();
        }

        public void RemoveRange(IEnumerable<OrderKoi> entities)
        {
            _context.OrderKois.RemoveRange(entities);
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
        public IQueryable<OrderKoi> GetAll()
        {
            return _context.OrderKois;
        }
    }
}
