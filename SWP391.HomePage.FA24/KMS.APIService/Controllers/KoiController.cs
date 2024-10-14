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
            var koiList = await _unitOfWork.KoiRepository.GetAllAsync();
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

            if (koi == null)
            {
                return BadRequest("Koi object is null");
            }


            if (string.IsNullOrEmpty(koi.Origin) || koi.KoiTypeId == null || koi.Age == null || koi.Size == null)
            {
                return BadRequest("Missing required fields.");
            }

            try
            {
                var koiType = await _unitOfWork.KoiTypeRepository.GetByIdAsync(koi.KoiTypeId.Value);
                if (koiType == null)
                {
                    return NotFound("KoiType not found.");
                }
                koi.Status = "available";
                koi.Name = koiType.Name;
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
            var koi = await _unitOfWork.KoiRepository.GetByIdAsync(id);
            if (koi == null)
            {
                return NotFound("Koi not found.");
            }
            if (koi.Status == "unavailable")
            {
                return BadRequest("Koi is unavailable already");
            }
            koi.Status = "unavailable";
            await _unitOfWork.KoiRepository.SaveAsync();

            return Ok(new { message = "Koi marked as deleted." });
        }
        [HttpPut("restore/{id}")]
        public async Task<IActionResult> RestoreKoi(int id)
        {

            var koi = await _unitOfWork.KoiRepository.GetByIdAsync(id);
            if (koi == null)
            {
                return NotFound("Koi not found.");
            }
            if (koi.Status != "unavailable")
            {
                return BadRequest("Koi is not unavailable.");
            }
            koi.Status = "available";
            await _unitOfWork.KoiRepository.SaveAsync();

            return Ok(new { message = "Fish marked as available." });
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
