using KMG.Repository.Base;
using KMG.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Repositories
{
    public class PointRepository : GenericRepository<Points>
    {
        public PointRepository(SwpkoiFarmShopContext context) : base(context) { }
        public async Task<List<Points>> GetPointsByUserIdAsync(int userId)
        {
            return await _context.Points
                                 .Where(p => p.UserId == userId)
                                 .OrderByDescending(p => p.TransactionType == "Earn")  // Ưu tiên giao dịch Earn
                                 .ToListAsync();
        }
        public async Task<List<Points>> GetPointsByOrderIdAsync(int orderId)
        {
            return await _context.Points
                .Where(pt => pt.OrderId == orderId)
                .ToListAsync();
        }

    }
}
