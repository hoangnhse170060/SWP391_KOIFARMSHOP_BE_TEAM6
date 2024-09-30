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
    public class UserRepository : GenericRepository<User>
    {
        public UserRepository(SwpkoiFarmShopContext context) : base(context) { }

        public User? Authenticate(string username, string password)
        {
            return _context.Users.FirstOrDefault(user => user.UserName == username && user.Password == password);
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            return await _context.Users.FirstOrDefaultAsync(user => user.UserName == username && user.Password == password);
        }
    }
}
