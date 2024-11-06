using KMG.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;

        public DashboardController(UnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("total-users")]
        public async Task<IActionResult> GetTotalUsers()
        {
            var totalUsers = await _unitOfWork.DashboardRepository.GetTotalUsersAsync();
            return Ok(new { TotalUsers = totalUsers });
        }

        [HttpGet("total-products")]
        public async Task<IActionResult> GetTotalProducts()
        {
            var totalProducts = await _unitOfWork.DashboardRepository.GetTotalProductsAsync();
            return Ok(new { TotalProducts = totalProducts });
        }

        [HttpGet("analysis")]
        public async Task<IActionResult> GetAnalysisData()
        {
            var analysisData = await _unitOfWork.DashboardRepository.GetAnalysisDataAsync();
            return Ok(analysisData);
        }
        [HttpGet("total-revenue")]
        public async Task<IActionResult> GetTotalRevenue()
        {
            var totalRevenue = await _unitOfWork.DashboardRepository.GetTotalRevenueAsync();
            return Ok(new { TotalRevenue = totalRevenue });
        }
        [HttpGet("order-analysis")]
        public async Task<IActionResult> GetOrderStatusStatistics()
        {
            var orderStatusStatistics = await _unitOfWork.DashboardRepository.GetOrderStatusStatisticsAsync();
            return Ok(orderStatusStatistics);
        }
        [HttpGet("top-users")]
        public async Task<IActionResult> GetTopUsers()
        {
            var topUsers = await _unitOfWork.DashboardRepository.GetTopUsersAsync();
            return Ok(topUsers);
        }

    }
}
