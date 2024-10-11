using KMG.Repository.Base;
using KMG.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Repositories
{
    public class OrderKoiRepository: GenericRepository<OrderKoi>
    {
        private readonly SwpkoiFarmShopContext _context;
        public OrderKoiRepository(SwpkoiFarmShopContext context) => _context = context;

    }
}
