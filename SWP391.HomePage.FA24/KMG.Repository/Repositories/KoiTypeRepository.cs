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
    public class KoiTypeRepository : GenericRepository<KoiType>
    {
        public KoiTypeRepository(SwpkoiFarmShopContext context) : base(context) { }

        public async Task<IEnumerable<KoiType>> GetKoiTypesAsync()
        {
            return await _context.KoiTypes
                .Include(k => k.Fish) 
                .Include(k => k.Kois) 
                .ToListAsync();
           
        }
    }
}
