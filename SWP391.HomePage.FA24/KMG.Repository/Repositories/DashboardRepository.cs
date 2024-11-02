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

        public async Task<decimal?> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.OrderStatus == "completed")
                .SumAsync(o => o.FinalMoney);
        }
        public async Task<int> GetTotalUsersAsync()
        {
            return await _context.Users.CountAsync();
        }


        public async Task<int> GetTotalProductsAsync()
        {
            var totalKoi = await _context.Kois.CountAsync();
            var totalFish = await _context.Fishes.CountAsync();
            return totalKoi + totalFish;
        }


        public async Task<object> GetAnalysisDataAsync()
        {

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
                })
                .OrderByDescending(x => x.TotalSold)
                .Join(_context.Kois,
                 topKoi => topKoi.KoiId,
                 koi => koi.KoiId,
                 (topKoi, koi) => new
                 {
                KoiId = topKoi.KoiId,
                KoiName = koi.Name,
                TotalSold = topKoi.TotalSold
                })
                .FirstOrDefaultAsync();


            var topSellingFish = await _context.OrderFishes
                .GroupBy(of => of.FishesId)
                .Select(g => new
                {
                    FishId = g.Key,
                    TotalSold = g.Sum(of => of.Quantity)
                })
                .OrderByDescending(x => x.TotalSold)
                .Join(_context.Fishes,
                topFish => topFish.FishId,
                fish => fish.FishesId,
                (topFish, fish) => new
                {
                FishId = topFish.FishId,
                FishName = fish.Name,
                TotalSold = topFish.TotalSold
                })
                .FirstOrDefaultAsync();
                

            return new
            {
                RevenuePerMonth = revenuePerMonth,
                TopSellingKoi = topSellingKoi,
                TopSellingFish = topSellingFish
            };
        }
        public async Task<object> GetOrderStatusStatisticsAsync()
        {
            var totalOrders = await _context.Orders.CountAsync();

            var orderStatusCounts = await _context.Orders
                .GroupBy(o => o.OrderStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Quantity = g.Count()
                })
                .ToListAsync();

            return new
            {
                TotalOrders = totalOrders,
                StatusCounts = orderStatusCounts
            };
        }

    }
}
