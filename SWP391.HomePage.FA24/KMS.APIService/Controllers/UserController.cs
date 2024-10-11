using KMG.Repository.Repositories;
using KMG.Repository.Base;
using Microsoft.EntityFrameworkCore;
using KMG.Repository;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        
        private readonly AddressRepository _addressRepository;
        private readonly UserRepository _userRepository;
        private readonly string _secretKey;

        public UserController(UnitOfWork unitOfWork)
        {
            _userRepository = new UserRepository(unitOfWork.UserRepository._context);
            _addressRepository = new AddressRepository(unitOfWork.AddressRepository._context);

            _secretKey = "xinchaocacbanminhlasang1234567890";
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>>
            GetUser()
        {
            var UserList = await _userRepository.GetAllAsync();
            Console.WriteLine($"Number of User retrieved: {UserList.Count}");
            return Ok(UserList);

        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            
            return Ok(user);
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] Login loginModel)
        {
            if (string.IsNullOrEmpty(loginModel.UserName) || string.IsNullOrEmpty(loginModel.Password))
            {
                return BadRequest("Username and Password are required.");
            }

            var user = await _userRepository.AuthenticateAsync(loginModel.UserName, loginModel.Password);

            if (user == null)
            {
                return Unauthorized("Invalid username or password.");
            }
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secretKey);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(ClaimTypes.Role, user.Role ?? "customer") 
                }),
                Expires = DateTime.UtcNow.AddDays(7), 
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);


            return Ok(new
            {
                Message = "Login successful",
                Token = tokenString,
                User = user
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] Register registerModel)
        {
           
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            
            var user = await _userRepository.RegisterAsync(registerModel.UserName, registerModel.Password, registerModel.Email);

            if (user == null)
            {
                return BadRequest("Username or Email already exists.");
            }

            return Ok(new
            {
                Message = "Registration successful",
                User = user
            });
        }

        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            
                var result = await _userRepository.GetByIdAsync(id);
                if (result==null)
                {
                    return NotFound(new { message = "User not found." });
                }
            result.Status = "locked";
            await _userRepository.SaveAsync();
            return Ok(new { message = "User is locked" });

        }
        [HttpPost("ChangePassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var users = await _userRepository.GetAll().ToListAsync();
            var user = users.FirstOrDefault(u => u.UserName == model.UserName);

            if (user == null)
            {
                return NotFound("User not found.");
            }

           
            user.Password = model.NewPassword;
            await _userRepository.SaveAsync();

            return Ok(new { Message = "Password changed successfully." });
        }
        [HttpPut("updateProfile{id}")]
        public async Task<IActionResult> UpdateProfile(int id, [FromBody] UpdateProfile model)
        {


            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            if (!string.IsNullOrEmpty(model.UserName))
            {
                user.UserName = model.UserName;
            }

            if (!string.IsNullOrEmpty(model.Email))
            {
                user.Email = model.Email;
            }

            if (!string.IsNullOrEmpty(model.PhoneNumber))
            {
                user.PhoneNumber = model.PhoneNumber;
            }

            if (!string.IsNullOrEmpty(model.Address))
            {
                var existingAddresses = await _addressRepository.GetAll()
             .Where(a => a.UserID == id).ToListAsync();

                foreach (var addr in existingAddresses)
                {
                    addr.IsDefault = false;
                    await _addressRepository.UpdateAsync(addr);
                }


                var newAddress = new Address
                {
                    UserID = id,
                    address = model.Address,
                    AddressType = "home",
                    IsDefault = true
                };

                await _addressRepository.CreateAsync(newAddress);
                user.Address = model.Address;
            }

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveAsync();
            await _addressRepository.SaveAsync();

            return Ok(new { Message = "Profile updated successfully.", User = user });
        }
        [HttpGet("getAddressesByUserId/{userId}")]
        public async Task<IActionResult> GetAddressesByUserId(int userId)
        {
           
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

           
            var addresses = await _addressRepository.GetAll()
                .Where(a => a.UserID == userId)
                .Select(a => new
                {
                    AddressID = a.AddressID,
                    Address = a.address,
                    AddressType = a.AddressType,
                    IsDefault = a.IsDefault
                }).ToListAsync();

           
            if (addresses == null || !addresses.Any())
            {
                return NotFound("No addresses found for this user.");
            }

            return Ok(addresses);
        }
        


    }
}
