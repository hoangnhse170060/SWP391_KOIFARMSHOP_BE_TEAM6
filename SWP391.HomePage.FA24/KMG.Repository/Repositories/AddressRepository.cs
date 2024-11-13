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
    public class AddressRepository : GenericRepository<Address>
    {
        private readonly SwpkoiFarmShopContext _context;
        public AddressRepository(SwpkoiFarmShopContext context) => _context = context;
        public IQueryable<Address> GetAll()
        {
            return _context.Address;
        }


    }
}
