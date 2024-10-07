using KMG.Repository.Models;
using KMG.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository
{
    public class UnitOfWork
    {
        private readonly SwpkoiFarmShopContext _context;
        private KoiRepository _koiRepository;
        private KoiTypeRepository _koiTypeRepository;
        private FishRepository _fishRepository;
        private PromotionRepository _promotionRepository;
        public  UnitOfWork() => _context = new SwpkoiFarmShopContext();
        public KoiRepository KoiRepository
        {
            get
            {
                return _koiRepository ??= new KoiRepository(_context);
            }
        }

        public KoiTypeRepository KoiTypeRepository
        {
            get
            {
                return _koiTypeRepository ??= new KoiTypeRepository(_context);
            }
        }
        public FishRepository FishRepository
        {
            get
            {
                return _fishRepository ??= new FishRepository(_context);
            }
        }
        public PromotionRepository PromotionRepository
        {
            get
            {
                return _promotionRepository ??= new PromotionRepository(_context);
            }
        }
    }
}
