using KMG.Repository;
using KMG.Repository.Models;
using KMG.Repository.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var koiList = await _unitOfWork.KoiRepository.GetAllKoisWithTypeAsync();
            Console.WriteLine($"Number of Koi retrieved: {koiList.Count}");
            return Ok(koiList);

        }
        [HttpGet("koitypes")]
        public async Task<ActionResult<IEnumerable<KoiType>>> GetKoiTypes()
        {
            var koiTypes = await _unitOfWork.KoiTypeRepository.GetKoiTypesAsync();



            return Ok(koiTypes);
        }

        [HttpPost]
        public async Task<ActionResult<Koi>> CreateKoi([FromBody] Koi koi)
        {
            // Kiểm tra xem đối tượng koi có null không
            if (koi == null)
            {
                return BadRequest("Koi object is null");
            }

            // Kiểm tra các thuộc tính bắt buộc
            if (string.IsNullOrEmpty(koi.Origin) || koi.KoiTypeId == null || koi.Age == null || koi.Size == null)
            {
                return BadRequest("Missing required fields.");
            }

            try
            {

                await _unitOfWork.KoiRepository.CreateAsync(koi);
                await _unitOfWork.KoiRepository.SaveAsync();


                return CreatedAtAction(nameof(GetKoi), new { id = koi.KoiId }, koi);
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
        public async Task<IActionResult> DeleteKoi(int id)
        {
            try
            {
                var result = await _unitOfWork.KoiRepository.DeleteWithId(id);
                if (result)
                {
                    return Ok(new { message = "Koi deleted successfully." });
                }
                return NotFound(new { message = "Koi not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateKoi(int id, [FromBody] Koi koi)
        {
            
            if (id != koi.KoiId)
            {
                return BadRequest("Koi ID mismatch.");
            }

            try
            {
               
                await _unitOfWork.KoiRepository.UpdateAsync(koi);
                await _unitOfWork.KoiRepository.SaveAsync(); 

                return Ok("Koi has been successfully updated.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound("The koi does not exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }





    }
}
