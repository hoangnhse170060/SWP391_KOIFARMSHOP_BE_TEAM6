﻿using KMG.Repository;
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
    }
}