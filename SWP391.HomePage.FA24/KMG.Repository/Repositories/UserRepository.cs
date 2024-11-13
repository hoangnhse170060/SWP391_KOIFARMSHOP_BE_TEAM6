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
            var hashedPassword = HashPassword.HashPasswordToSha256(password);
            return await _context.Users.FirstOrDefaultAsync(user => user.UserName == username && user.Password == hashedPassword);
        }
        public async Task<User?> RegisterAsync(string username, string password, string email)
        {

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(user => user.UserName == username || user.Email == email);
            if (existingUser != null)
                return null;
            var hashedPassword = HashPassword.HashPasswordToSha256(password);
            var newUser = new User
            {
                UserName = username,
                Password = hashedPassword,
                Email = email,
                Role = "customer",
                Status = "active",
                PhoneNumber = null,
                Address = null,
                RegisterDate = DateOnly.FromDateTime(DateTime.Now),
                TotalPoints = 0
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }
        public async Task<User?> RegisterStaffAsync(string username, string password, string email)
        {

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(user => user.UserName == username || user.Email == email);
            if (existingUser != null)
                return null;
            var hashedPassword = HashPassword.HashPasswordToSha256(password);
            var newUser = new User
            {
                UserName = username,
                Password = hashedPassword,
                Email = email,
                Role = "staff",
                Status = "active",
                PhoneNumber = null,
                Address = null,
                RegisterDate = DateOnly.FromDateTime(DateTime.Now),
                TotalPoints = 0
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }
        public IQueryable<User> GetAll()
        {
            return _context.Users;
        }
        public async Task<User?> RegisterGoogle(string username, string email)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(user => user.UserName == username || user.Email == email);

            if (existingUser != null)
                return null;

            var newUser = new User
            {
                UserName = username ?? email,
                Email = email,
                Password = Guid.NewGuid().ToString(),
                Role = "customer",
                Status = "active",
                PhoneNumber = null,
                Address = null,
                RegisterDate = DateOnly.FromDateTime(DateTime.Now),
                TotalPoints = 0
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return newUser;
        }


    }
}
