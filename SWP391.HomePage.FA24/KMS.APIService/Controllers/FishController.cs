using KMG.Repository.Models;
using KMG.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KMG.Repository.Repositories;

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


            if (fish.KoiTypeId == null || fish.Quantity == null || fish.Price == null)
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
                fish.Status = "available";
                fish.Name = koiType.Name;
                await _unitOfWork.FishRepository.CreateAsync(fish);
                await _unitOfWork.FishRepository.SaveAsync();


                return CreatedAtAction(nameof(GetFish), new { id = fish.FishesId }, fish);
            }

            catch (Exception ex)
            {

                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFish(int id)
        {
            var fish = await _unitOfWork.FishRepository.GetByIdAsync(id);
            if (fish == null)
            {
                return NotFound("Fish not found.");
            }
            if (fish.Status == "unavailable")
            {
                return BadRequest("Fish is  unavailable already");
            }
            fish.Status = "unavailable";
            await _unitOfWork.FishRepository.SaveAsync();

            return Ok(new { message = "Fish marked as deleted." });
        }
        [HttpPut("restore/{id}")]
        public async Task<IActionResult> RestoreFish(int id)
        {

            var fish = await _unitOfWork.FishRepository.GetByIdAsync(id);
            if (fish == null)
            {
                return NotFound("Fish not found.");
            }
            if (fish.Status != "unavailable")
            {
                return BadRequest("Fish is not unavailable.");
            }
            fish.Status = "available";
            await _unitOfWork.FishRepository.SaveAsync();

            return Ok(new { message = "Fish marked as available." });
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
