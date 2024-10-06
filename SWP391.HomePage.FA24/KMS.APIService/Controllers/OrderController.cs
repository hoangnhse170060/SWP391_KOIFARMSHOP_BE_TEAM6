using KMG.Repository.Models;
using KMG.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;
        public  OrderController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

         [HttpGet]
         public async Task<ActionResult<IEnumerable<Order>>> GetOrder()
            {
                var orders = await _unitOfWork.OrderRepository.GetAllAsync();
                return Ok(orders);
            }

        


        //public class KoiController : ControllerBase
        //{
        //    private readonly UnitOfWork _unitOfWork;
        //    public KoiController(UnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        //    [HttpGet]
        //    public async Task<ActionResult<IEnumerable<Koi>>>
        //        GetKoi()
        //    {
        //        return await _unitOfWork.KoiRepository.GetAllAsync();
        //    }

    }
    }
