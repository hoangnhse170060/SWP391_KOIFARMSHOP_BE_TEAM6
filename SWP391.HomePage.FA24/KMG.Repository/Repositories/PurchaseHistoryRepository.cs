using KMG.Repository.Base;
using KMG.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Repositories
{
    public class PurchaseHistoryRepository:GenericRepository<PurchaseHistory>
    {
        private readonly SwpkoiFarmShopContext _context;
        public PurchaseHistoryRepository(SwpkoiFarmShopContext context) => _context = context;
        public IQueryable<PurchaseHistory> GetAll()
        {
            return _context.PurchaseHistories;
        }
        public IQueryable<object> GetAllWithDetails()
        {
            return _context.PurchaseHistories
                .Include(p => p.Order)                     
                .ThenInclude(o => o.OrderKois)             
                .ThenInclude(ok => ok.Koi)                 
                .Include(p => p.Order.OrderFishes)           
                .ThenInclude(of => of.Fishes)
                .Include(p=>p.User)
                .Select(p => new
                {
                    Username=p.User.UserName,
                    p.OrderId,
                    p.PurchaseDate,
                    p.TotalMoney,
                    p.DiscountMoney,
                    p.FinalMoney,
                    p.OrderStatus,
                    p.PaymentMethod,
                    p.ShippingDate,
                    p.DeliveryStatus,
                    p.PromotionId,
                    p.EarnedPoints,
                    p.UsedPoints,
                    KoiDetails = p.Order.OrderKois.Select(ok => new
                    {
                        ok.Koi.Name,
                        ok.Koi.ImageKoi,    
                        ok.Quantity        
                    }).ToList(),
                    FishDetails = p.Order.OrderFishes.Select(of => new
                    {
                        of.Fishes.Name,
                        of.Fishes.ImageFishes,  
                        of.Quantity         
                    }).ToList()
                });
        }
    }
}
