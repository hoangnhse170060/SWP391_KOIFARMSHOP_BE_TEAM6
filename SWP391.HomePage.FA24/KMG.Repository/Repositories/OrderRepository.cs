using KMG.Repository.Base;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Repositories
{
    public class OrderRepository : GenericRepository<Order>
    {
        private readonly SwpkoiFarmShopContext _context;
        public OrderRepository(SwpkoiFarmShopContext context) => _context = context;


        public async Task<Order> GetOrderWithDetailsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderKois)  // Load các OrderKois liên quan
                .ThenInclude(ok => ok.Koi)  // Load thông tin Koi của OrderKoi
                .Include(o => o.OrderFishes)  // Load các OrderFishes liên quan
                .ThenInclude(of => of.Fishes)  // Load thông tin Fish của OrderFish
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }


        public void Delete(Order entity)
        {
            _context.Orders.Remove(entity);
        }

        public void RemoveRange(IEnumerable<Order> orders)
        {
            _context.Orders.RemoveRange(orders);
        }


        public async Task<Order> FirstOrDefaultAsync(Expression<Func<Order, bool>> predicate)
        {
            return await _context.Orders.FirstOrDefaultAsync(predicate);
        }

    }
}






