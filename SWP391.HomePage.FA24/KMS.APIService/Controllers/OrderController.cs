using KMG.Repository.Models;
using KMG.Repository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KMG.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly ILogger<OrderController> _logger;

        public OrderController(UnitOfWork unitOfWork, ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        //Show all Order
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var orders = await _unitOfWork.OrderRepository.GetAllAsync();
            return Ok(orders);
        }
        //Create Order 
        [HttpPost]
        public async Task<ActionResult<Koi>> CreateOrder([FromBody] Order order)
        {
            if (order == null)
            {
                return BadRequest("Koi object is null");
            }
            try
            {
                await _unitOfWork.OrderRepository.CreateAsync(order);
                await _unitOfWork.OrderRepository.SaveAsync();
                return CreatedAtAction(nameof(GetOrders), new { id = order.OrderId }, order);
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

        //UpdateOrder
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOrder(int id, [FromBody] Order order)
        {
            if (id != order.OrderId)
            {
                return BadRequest("Order ID mismatch.");
            }
            try
            {
                await _unitOfWork.OrderRepository.UpdateAsync(order);
                await _unitOfWork.OrderRepository.SaveAsync();
                return Ok("Order has been successfully updated.");
            }
            catch (DbUpdateConcurrencyException)
            {
                return NotFound("The order does not exist.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating the order");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            try
            {
                var orderToDelete = await _unitOfWork.OrderRepository.GetByIdAsync(id);

                if (orderToDelete == null)
                {
                    return NotFound($"Order with Id = {id} not found");
                }

                // Delete the order
                _unitOfWork.OrderRepository.Remove(orderToDelete);
                await _unitOfWork.OrderRepository.SaveAsync();  // Persist changes in the database

                return NoContent();  // Return a NoContent status when deletion is successful
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    $"Error deleting order: {ex.Message}");
            }
        }


    }



}

