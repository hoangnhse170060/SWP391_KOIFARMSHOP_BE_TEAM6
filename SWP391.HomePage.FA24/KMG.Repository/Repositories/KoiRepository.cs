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
    public class KoiRepository:GenericRepository<Koi>
    {
        private readonly SwpkoiFarmShopContext _context;
        public KoiRepository(SwpkoiFarmShopContext context) =>_context = context;
       
        public async Task<bool> DeleteWithId(int koiId)
        {
            var koi = await _context.Kois.FindAsync(koiId);
            if (koi != null)
            {
                await RemoveAsync(koi);
                return true;
            }
            return false;


        }
    }
    
}
