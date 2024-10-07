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
        public async Task<User?> RegisterAsync(string username, string password, string email)
        {
            // Check if the username or email already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(user => user.UserName == username || user.Email == email);
            if (existingUser != null)
                return null; 

            
            var newUser = new User
            {
                UserName = username,
                Password = password,
                Email = email,
                Role = "customer", 
                Status = "active", 
                RegisterDate = DateOnly.FromDateTime(DateTime.Now),
                TotalPoints = 0 
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }
        public async Task<bool> DeleteWithId(int userID)
        {
            var user = await _context.Users.FindAsync(userID);
            if (user != null)
            {
                await RemoveAsync(user);
                return true;
            }
            return false;


        }
    }
}
