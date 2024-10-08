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
                return BadRequest("Koi object is null");
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

                
                await _unitOfWork.PromotionRepository.CreateAsync(promotion);
                await _unitOfWork.PromotionRepository.SaveAsync();


                return CreatedAtAction(nameof(GetPromotion), new { id = promotion.PromotionId }, promotion);
                
            }
            catch (DbUpdateException dbEx)
            {

                var innerException = dbEx.InnerException != null ? dbEx.InnerException.Message : "No inner exception";
                return StatusCode(500, $"Database update error: {dbEx.Message}, Inner Exception: {innerException}");
            }
            catch (Exception ex)
            {

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePromotion(int id)
        {
            try
            {
                var result = await _unitOfWork.PromotionRepository.DeleteWithId(id);
                if (result)
                {
                    return Ok(new { message = "Promotion deleted successfully." });
                }
                return NotFound(new { message = "Promotion not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePromotion(int id, [FromBody] Promotion promotion)
        {

            if (id != promotion.PromotionId)
            {
                return BadRequest("Promotion ID mismatch.");
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
