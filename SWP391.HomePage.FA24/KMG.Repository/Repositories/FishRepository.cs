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
    public class FishRepository:GenericRepository<Fish>
    {
        private readonly SwpkoiFarmShopContext _context;
        public FishRepository(SwpkoiFarmShopContext context) => _context = context;
        
       

    }
}
