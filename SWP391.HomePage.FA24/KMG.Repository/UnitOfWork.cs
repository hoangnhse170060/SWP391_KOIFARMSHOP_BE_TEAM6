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
        public  UnitOfWork() => _context = new SwpkoiFarmShopContext();
        public KoiRepository KoiRepository
        {
            get
            {
                return _koiRepository ??= new KoiRepository(_context);
            }
        }
    }
}
