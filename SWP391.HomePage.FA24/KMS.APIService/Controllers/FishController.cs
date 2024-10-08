using KMG.Repository.Models;
using KMG.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FishController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;
        public FishController(UnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Fish>>>
            GetFish()
        {
            var fishList = await _unitOfWork.FishRepository.GetAllAsync();
            Console.WriteLine($"Number of Fish retrieved: {fishList.Count}");
            return Ok(fishList);

        }

        [HttpPost]
        public async Task<ActionResult<Fish>> CreateFish([FromBody] Fish fish)
        {
            
            if (fish == null)
            {
                return BadRequest("Koi object is null");
            }

       
            if ( fish.KoiTypeId == null || fish.Quantity == null || fish.Price == null)
            {
                return BadRequest("Missing required fields.");
            }

            try
            {
                var koiType = await _unitOfWork.KoiTypeRepository.GetByIdAsync(fish.KoiTypeId.Value);
                if (koiType == null)
                {
                    return NotFound("KoiType not found.");
                }
                fish.Name = koiType.Name;
                await _unitOfWork.FishRepository.CreateAsync(fish);
                await _unitOfWork.FishRepository.SaveAsync();


                return CreatedAtAction(nameof(GetFish), new { id = fish.FishesId }, fish);
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
        public async Task<IActionResult> DeleteFish(int id)
        {
            try
            {
                var result = await _unitOfWork.FishRepository.DeleteWithId(id);
                if (result)
                {
                    return Ok(new { message = "Fish deleted successfully." });
                }
                return NotFound(new { message = "Fish not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFish(int id, [FromBody] Fish fish)
        {
            
            if (id != fish.FishesId)
            {
                return BadRequest("Fish ID mismatch.");
            }

            try
            {
               
                await _unitOfWork.FishRepository.UpdateAsync(fish);
                await _unitOfWork.FishRepository.SaveAsync(); 

                return Ok("Koi has been successfully updated.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound("The fish does not exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
