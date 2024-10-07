using KMG.Repository.Repositories;
using KMG.Repository.Base;
using KMG.Repository;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserRepository _userRepository;
        private readonly string _secretKey;

        public UserController(UnitOfWork unitOfWork)
        {
            _userRepository = new UserRepository(unitOfWork.KoiRepository._context);
            _secretKey = "xinchaocacbanminhlasang1234567890";
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

        [AcceptVerbs("GET", "POST")]
        [Route("IsEmailAlreadyRegister")]
        public IActionResult IsEmailAlreadyRegister(string email)
        {
            var user = _userRepository.GetAll().FirstOrDefault(u => u.Email == email);
            if (user != null)
            {
                return BadRequest(new { Message = "Email is already used" }); 
            }
            return Ok(new { Message = "Email is available" }); 
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKoi(int id)
        {
            try
            {
                var result = await _userRepository.DeleteWithId(id);
                if (result)
                {
                    return Ok(new { message = "User deleted successfully." });
                }
                return NotFound(new { message = "User not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
