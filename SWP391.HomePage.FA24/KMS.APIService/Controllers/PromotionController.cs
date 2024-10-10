using KMG.Repository;
using KMG.Repository.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PromotionController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;
        public PromotionController(UnitOfWork unitOfWork) => _unitOfWork = unitOfWork;
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Promotion>>> GetPromotion()
        {
            return await _unitOfWork.PromotionRepository.GetAllAsync();
        }
        [HttpPost]
        public async Task<ActionResult<Promotion>> CreatePromotion([FromBody] Promotion promotion)
        {
            
            if (promotion == null)
            {
                return BadRequest("Promtotion object is null");
            }

            if (string.IsNullOrEmpty(promotion.PromotionName) ||
            string.IsNullOrEmpty(promotion.Description) ||
            promotion.DiscountRate == null ||
            promotion.StartDate == default ||
            promotion.EndDate == default)
            {
                return BadRequest("All fields except PromotionId are required.");
            }

            
            if (promotion.EndDate <= promotion.StartDate)
            {
                return BadRequest("EndDate must be greater than StartDate.");
            }

            try
            {

                promotion.Status = true ;
                await _unitOfWork.PromotionRepository.CreateAsync(promotion);
                await _unitOfWork.PromotionRepository.SaveAsync();


                return CreatedAtAction(nameof(GetPromotion), new { id = promotion.PromotionId }, promotion);
                
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            var fish = await _unitOfWork.PromotionRepository.GetByIdAsync(id);
            if (fish == null)
            {
                return NotFound("Promotion not found.");
            }

            fish.Status = false ;
            await _unitOfWork.PromotionRepository.SaveAsync();

            return Ok(new { message = "Promotion marked as deleted." });
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] Promotion promotion)
        {

            if (id != promotion.PromotionId)
            {
                return BadRequest("Promotion ID mismatch.");
            }
            if (promotion.EndDate <= promotion.StartDate)
            {
                return BadRequest("EndDate must be greater than StartDate.");
            }
            try
            {

                await _unitOfWork.PromotionRepository.UpdateAsync(promotion);
                await _unitOfWork.PromotionRepository.SaveAsync();

                return Ok("Promotion has been successfully updated.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound("The promotion does not exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
