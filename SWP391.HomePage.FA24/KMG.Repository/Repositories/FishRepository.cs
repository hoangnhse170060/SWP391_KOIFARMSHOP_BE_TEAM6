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
        public async Task<List<Fish>> GetAllFishWithTypeAsync()
        {
            return await _context.Fishes
                .Include(k => k.KoiType)
                .OrderBy(k => k.FishesId)
                .ToListAsync();
        }
        public async Task<bool> DeleteWithId(int fishesID)
        {
            var fish = await _context.Fishes.FindAsync(fishesID);
            if (fish != null)
            {
                await RemoveAsync(fish);
                return true;
            }
            return false;


        }

    }
}
