using KMG.Repository.Base;
using KMG.Repository.Models;
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
    }
}
