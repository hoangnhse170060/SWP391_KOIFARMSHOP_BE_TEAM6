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
        [HttpGet("{id}")]
        public async Task<IActionResult> GetKoiById(int id)
        {

            var koi = await _unitOfWork.KoiRepository.GetByIdAsync(id);
            if (koi == null)
            {
                return NotFound(new { message = "Koi not found." });
            }


            return Ok(koi);
        }
        [HttpGet("koitypes")]
        public async Task<ActionResult<IEnumerable<KoiType>>> GetKoiTypes()
        {
            var koiTypes = await _unitOfWork.KoiTypeRepository.GetKoiTypesAsync();
            return Ok(koiTypes);
        }
        [HttpPost("createKoiType")]
        public async Task<ActionResult<KoiType>> CreateKoiType([FromBody] KoiType koiType)
        {
            if (koiType == null)
            {
                return BadRequest("Koi Type is null.");
            }
            var exitKoiType = await _unitOfWork.KoiTypeRepository.GetByNameAsync(koiType.Name);
            if (exitKoiType!=null)
            {
                return BadRequest("Koi Type Already Have !");
            }
            await _unitOfWork.KoiTypeRepository.CreateAsync(koiType);
            await _unitOfWork.KoiTypeRepository.SaveAsync();
            return Ok(koiType);
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
                koi.quantityInStock = 1;
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

                var result = await _unitOfWork.KoiRepository.UpdateKoiAsync(id, koi);
                if (!result)
                {
                    return NotFound("The koi does not exist.");
                }

                return Ok("Koi has been successfully updated.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }








    }
}
