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
        public async Task<bool> UpdateKoiAsync(int id, Koi koi)
        {
            var existingKoi = await _context.Kois.FindAsync(id);
            if (existingKoi == null)
            {
                return false;
            }

           
            bool koiTypeChanged = false;
            if (koi.KoiTypeId != null && koi.KoiTypeId != existingKoi.KoiTypeId)
            {
                koiTypeChanged = true;
                existingKoi.KoiTypeId = koi.KoiTypeId;
            }

            existingKoi.Name = !string.IsNullOrEmpty(koi.Name) ? koi.Name : existingKoi.Name;
            existingKoi.Origin = !string.IsNullOrEmpty(koi.Origin) ? koi.Origin : existingKoi.Origin;
            existingKoi.Gender = !string.IsNullOrEmpty(koi.Gender) ? koi.Gender : existingKoi.Gender;
            existingKoi.Age = koi.Age ?? existingKoi.Age;
            existingKoi.Size = koi.Size ?? existingKoi.Size;
            existingKoi.Breed = !string.IsNullOrEmpty(koi.Breed) ? koi.Breed : existingKoi.Breed;
            existingKoi.Personality = !string.IsNullOrEmpty(koi.Personality) ? koi.Personality : existingKoi.Personality;
            existingKoi.FeedingAmount = koi.FeedingAmount ?? existingKoi.FeedingAmount;
            existingKoi.FilterRate = koi.FilterRate ?? existingKoi.FilterRate;
            existingKoi.HealthStatus = !string.IsNullOrEmpty(koi.HealthStatus) ? koi.HealthStatus : existingKoi.HealthStatus;
            existingKoi.AwardCertificates = !string.IsNullOrEmpty(koi.AwardCertificates) ? koi.AwardCertificates : existingKoi.AwardCertificates;
            existingKoi.Status = !string.IsNullOrEmpty(koi.Status) ? koi.Status : existingKoi.Status;
            existingKoi.Price = koi.Price ?? existingKoi.Price;
            existingKoi.QuantityInstock = koi.QuantityInstock != 0 ? koi.QuantityInstock : existingKoi.QuantityInstock;
            existingKoi.ImageKoi = !string.IsNullOrEmpty(koi.ImageKoi) ? koi.ImageKoi : existingKoi.ImageKoi;
            existingKoi.ImageCertificate = !string.IsNullOrEmpty(koi.ImageCertificate) ? koi.ImageCertificate : existingKoi.ImageCertificate;
            existingKoi.Description = !string.IsNullOrEmpty(koi.Description) ? koi.Description : existingKoi.Description;
            existingKoi.DetailDescription = !string.IsNullOrEmpty(koi.DetailDescription) ? koi.DetailDescription : existingKoi.DetailDescription;
            existingKoi.AdditionImage = !string.IsNullOrEmpty(koi.AdditionImage) ? koi.AdditionImage : existingKoi.AdditionImage;

            if (koiTypeChanged)
            {
                var koiType = await _context.KoiTypes.FindAsync(koi.KoiTypeId.Value);
                if (koiType == null)
                {
                    throw new Exception("The koi type does not exist.");
                }
                existingKoi.Name = koiType.Name;
            }

            _context.Kois.Update(existingKoi);
            await _context.SaveChangesAsync();

            return true;
        }

    }
    
}
