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
    public class FishRepository : GenericRepository<Fish>
    {
        private readonly SwpkoiFarmShopContext _context;
        public FishRepository(SwpkoiFarmShopContext context) => _context = context;
        public async Task<bool> UpdateFishAsync(int id, Fish fish)
        {
            var existingFish = await _context.Fishes.FindAsync(id);
            if (existingFish == null)
            {
                return false;
            }
            bool koiTypeChanged = false;
            if (fish.KoiTypeId != null && fish.KoiTypeId != existingFish.KoiTypeId)
            {
                koiTypeChanged = true;
                existingFish.KoiTypeId = fish.KoiTypeId;
            }
            existingFish.Quantity = fish.Quantity ?? existingFish.Quantity;
            existingFish.Status = !string.IsNullOrEmpty(fish.Status) ? fish.Status : existingFish.Status;
            existingFish.Price = fish.Price ?? existingFish.Price;
            existingFish.quantityInStock = fish.quantityInStock != 0 ? fish.quantityInStock : existingFish.quantityInStock;
            existingFish.ImageFishes = !string.IsNullOrEmpty(fish.ImageFishes) ? fish.ImageFishes : existingFish.ImageFishes;
            existingFish.Description = !string.IsNullOrEmpty(fish.Description) ? fish.Description : existingFish.Description;
            existingFish.DetailDescription = !string.IsNullOrEmpty(fish.DetailDescription) ? fish.DetailDescription : existingFish.DetailDescription;
            if (koiTypeChanged)
            {
                var koiType = await _context.KoiTypes.FindAsync(fish.KoiTypeId.Value);
                if (koiType == null)
                {
                    throw new Exception("The koi type does not exist.");
                }
                existingFish.Name = koiType.Name;
            }

            _context.Fishes.Update(existingFish);
            await _context.SaveChangesAsync();

            return true;
        }


    }
}
