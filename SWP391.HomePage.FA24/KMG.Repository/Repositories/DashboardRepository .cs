using KMG.Repository.Base;
using KMG.Repository.Models;
using KMG.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace KMG.Repository.Repositories
{
    public class DashboardRepository
    {
        private readonly SwpkoiFarmShopContext _context;

        public DashboardRepository(SwpkoiFarmShopContext context)
        {
            _context = context;
        }

        // Lấy tổng số user
        public async Task<int> GetTotalUsersAsync()
        {
            return await _context.Users.CountAsync();
        }

        // Lấy tổng số sản phẩm (Koi và Fish)
        public async Task<int> GetTotalProductsAsync()
        {
            var totalKoi = await _context.Kois.CountAsync();
            var totalFish = await _context.Fishes.CountAsync();
            return totalKoi + totalFish;
        }

        // Lấy dữ liệu phân tích: Doanh thu theo tháng và sản phẩm bán chạy nhất
        public async Task<object> GetAnalysisDataAsync()
        {
            // Doanh thu theo tháng
            var revenuePerMonth = await _context.Orders
                .GroupBy(o => o.OrderDate.Value.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    TotalRevenue = g.Sum(o => o.FinalMoney)
                }).ToListAsync();

            
            var topSellingKoi = await _context.OrderKois
                .GroupBy(ok => ok.KoiId)
                .Select(g => new
                {
                    KoiId = g.Key,
                    TotalSold = g.Sum(ok => ok.Quantity)
                }).OrderByDescending(x => x.TotalSold)
                .FirstOrDefaultAsync();

            
            var topSellingFish = await _context.OrderFishes
                .GroupBy(of => of.FishesId)
                .Select(g => new
                {
                    FishId = g.Key,
                    TotalSold = g.Sum(of => of.Quantity)
                }).OrderByDescending(x => x.TotalSold)
                .FirstOrDefaultAsync();

           
            return new
            {
                RevenuePerMonth = revenuePerMonth,
                TopSellingKoi = topSellingKoi,
                TopSellingFish = topSellingFish
            };
        }
    }
}
