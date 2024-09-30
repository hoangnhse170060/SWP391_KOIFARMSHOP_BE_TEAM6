using KMG.Repository;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Mvc;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KoiController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;
        public KoiController(UnitOfWork unitOfWork) => _unitOfWork = unitOfWork;
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Koi>>>
            GetKoi()
        {
            return await _unitOfWork.KoiRepository.GetAllAsync();
        }

    }
}
