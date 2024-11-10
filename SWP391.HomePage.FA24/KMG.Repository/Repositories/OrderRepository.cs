using KMG.Repository.Base;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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
                .Include(o => o.User) // Bao gồm thông tin User
                .Include(o => o.OrderFishes).ThenInclude(f => f.Fishes)
                .Include(o => o.OrderKois).ThenInclude(k => k.Koi)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }


        public async Task<List<Order>> GetOrdersByUserNameAsync(string name)
        {
            return await _context.Orders
                .Include(o => o.User) // Include the User entity to access UserName
                .Include(o => o.OrderKois)
                .ThenInclude(ok => ok.Koi)
                .Include(o => o.OrderFishes)
                .ThenInclude(of => of.Fishes)
                .Where(o => o.User.UserName == name) // Filter by User.UserName
                .ToListAsync(); // Return a list of orders
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


        public IQueryable<Order> GetAll()
        {
            return _context.Orders;
        }
        public async Task<bool> IsUserOrderOwnerAsync(int userId, int orderId)
        {
            return await _context.Orders.AnyAsync(o => o.OrderId == orderId && o.UserId == userId);
        }




    }
}






