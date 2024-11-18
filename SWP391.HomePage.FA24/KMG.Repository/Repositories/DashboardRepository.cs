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
        public async Task<Object> GetTotalUsersAsync()
        {
            var staffAcount = await _context.Users.CountAsync(s => s.Role == "staff");
            var userAccount = await _context.Users.CountAsync(u => u.Role == "customer");
            var totalAccount = await _context.Users.CountAsync();
            return new
            {
                Staff_account = staffAcount,
                User_account = userAccount,
                Totak_account = totalAccount,
            };
        }


        public async Task<object> GetTotalProductsAsync()
        {
            var totalKoi = await _context.Kois.CountAsync();
            var totalFish = await _context.Fishes.CountAsync();

            var availableKoi = await _context.Kois.CountAsync(k => k.Status == "Available");
            var unavailableKoi = totalKoi - availableKoi;

            var availableFish = await _context.Fishes.CountAsync(f => f.Status == "Available");
            var unavailableFish = totalFish - availableFish;

            return new
            {
                TotalProducts = totalKoi + totalFish,
                TotalKoi = new { TotalKoi = totalKoi, Available = availableKoi, Unavailable = unavailableKoi },
                TotalFish = new { TotalFish = totalFish, Available = availableFish, Unavailable = unavailableFish }
            };
        }
        public async Task<Dictionary<string, decimal>> GetRevenueByAllDatesAsync()
        {
            var revenuePerDay = await _context.Orders
                .Where(o => o.OrderStatus == "completed" && o.OrderDate.HasValue)
                .ToListAsync();

            var groupedRevenue = revenuePerDay
                .GroupBy(o => o.OrderDate.Value.ToDateTime(TimeOnly.MinValue).Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    TotalRevenue = g.Sum(o => o.FinalMoney) ?? 0
                })
                .ToDictionary(x => x.Date, x => x.TotalRevenue);

            return groupedRevenue;
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
            var feedbackStatistics = await _context.Feedbacks
                .GroupBy(f => 1)
                .Select(g => new
                {
                    TotalFeedbacks = g.Count(),
                    AverageRating = g.Average(f => (double?)f.Rating) ?? 0
                })
                .FirstOrDefaultAsync();


            return new
            {
                RevenuePerMonth = revenuePerMonth,
                TopSellingKoi = topSellingKoi,
                TopSellingFish = topSellingFish,
                FeedbackStatistics = feedbackStatistics
            };
        }
        public async Task<object> GetOrderStatusStatisticsAsync()
        {
            var orderStatusCounts = await _context.Orders
                .Where(o => o.OrderDate.HasValue) 
                .GroupBy(o => new { o.OrderStatus, Month = o.OrderDate.Value.Month, Year = o.OrderDate.Value.Year })
                .Select(g => new
                {
                    Status = g.Key.OrderStatus,
                    Month = g.Key.Month,
                    Year = g.Key.Year,
                    Quantity = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            return orderStatusCounts;
        }

        public async Task<object> GetTopUsersAsync(int topCount = 3)
        {
            var topUsers = await _context.Users
                .Select(user => new
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    TotalOrders = _context.Orders.Count(o => o.UserId == user.UserId),
                    TotalSpent = _context.Orders
                        .Where(o => o.UserId == user.UserId && o.OrderStatus == "completed")
                        .Sum(o => (decimal?)o.FinalMoney) ?? 0
                })
                .OrderByDescending(user => user.TotalSpent)
                .Take(topCount)
                .ToListAsync();

            return topUsers;
        }


    }
}




// HOÀNG ĂN CỨC 