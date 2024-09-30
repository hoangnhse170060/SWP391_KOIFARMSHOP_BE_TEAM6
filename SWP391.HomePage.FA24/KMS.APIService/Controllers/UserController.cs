using KMG.Repository.Repositories;
using KMG.Repository;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Mvc;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserRepository _userRepository;

        public UserController(UnitOfWork unitOfWork)
        {
            _userRepository = new UserRepository(unitOfWork.KoiRepository._context);
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

           
            return Ok(new
            {
                Message = "Login successful",
                User = user
            });
        }
    }
}
