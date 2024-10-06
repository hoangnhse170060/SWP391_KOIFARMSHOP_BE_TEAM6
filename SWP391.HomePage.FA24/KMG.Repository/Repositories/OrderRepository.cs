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
    public class OrderRepository : GenericRepository<Order>
    {
        public OrderRepository(SwpkoiFarmShopContext context) => _context = context;
    }
}
