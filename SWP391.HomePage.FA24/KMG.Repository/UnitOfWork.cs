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
        private OrderRepository _orderRepository;
        public  UnitOfWork() => _context = new SwpkoiFarmShopContext();
        public KoiRepository KoiRepository
        {
            get
            {
                return _koiRepository ??= new KoiRepository(_context);
            }
        }


        public OrderRepository OrderRepository
        {
            get
            {
                return _orderRepository ??= new OrderRepository(_context);
               
            }
        }

        public void Complete()
        {
            throw new NotImplementedException();
        }

        public async Task SaveAsync()
        {
            throw new NotImplementedException();
        }

        public async Task SaveChangesAsync()
        {
            throw new NotImplementedException();
        }
    }
}
