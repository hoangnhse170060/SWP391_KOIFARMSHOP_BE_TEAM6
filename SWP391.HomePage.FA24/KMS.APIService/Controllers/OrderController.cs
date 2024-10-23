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
using Microsoft.EntityFrameworkCore.Storage;

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

        [HttpGet("{id:int}")]
        public async Task<ActionResult<object>> GetOrderById(int id)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(id);

                if (order == null)
                {
                    return NotFound($"Order with Id = {id} not found.");
                }

                // Chuẩn bị dữ liệu trả về
                var result = new
                {
                    order.OrderId,
                    order.UserId,
                    order.OrderDate,
                    order.TotalMoney,
                    order.FinalMoney,
                    order.OrderStatus,
                    order.PaymentMethod,
                    Fishes = order.OrderFishes.Select(f => new
                    {
                        f.FishesId,
                        f.Quantity,
                        f.Fishes.Name,
                        f.Fishes.Status,
                        f.Fishes.Price,
                        f.Fishes.ImageFishes
                    }),
                    Kois = order.OrderKois.Select(k => new
                    {
                        k.KoiId,
                        k.Quantity,
                        k.Koi.Name,
                        k.Koi.Gender,
                        k.Koi.Price,
                        k.Koi.Size,
                        k.Koi.ImageKoi
                    })
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order by ID.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        //Show all Order
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var orders = await _unitOfWork.OrderRepository.GetAllAsync();
            return Ok(orders);
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
        {
            if (order == null)
            {
                return BadRequest("Order object is null.");
            }

            // Kiểm tra: Phải chọn hoặc OrderFishes hoặc OrderKois, không được để trống cả hai.
            if ((order.OrderFishes == null || !order.OrderFishes.Any()) &&
                (order.OrderKois == null || !order.OrderKois.Any()))
            {
                return BadRequest("You must choose either Order Fishes or Order Kois.");
            }


            using var transaction = await _unitOfWork.OrderRepository.BeginTransactionAsync();

            try
            {
                decimal totalMoney = 0;

                // **Xử lý OrderKois**
                if (order.OrderKois != null && order.OrderKois.Any())
                {
                    foreach (var orderKoi in order.OrderKois)
                    {
                        var koiEntity = await _unitOfWork.KoiRepository.GetByIdAsync(orderKoi.KoiId);
                        if (koiEntity == null)
                        {
                            return NotFound($"Koi with ID = {orderKoi.KoiId} not found.");
                        }

                        // Kiểm tra số lượng trong kho có đủ không
                        if (koiEntity.quantityInStock < orderKoi.Quantity)
                        {
                            return BadRequest($"Not enough stock for Koi ID = {orderKoi.KoiId}. " +
                                              $"Requested: {orderKoi.Quantity}, Available: {koiEntity.quantityInStock}");
                        }

                        // Trừ số lượng Koi trong kho
                        koiEntity.quantityInStock -= orderKoi.Quantity;

                        // Tính tổng tiền từ Koi với chuyển đổi rõ ràng
                        totalMoney += koiEntity.Price.GetValueOrDefault(0) * (orderKoi.Quantity ?? 0);
                        // Cập nhật vào cơ sở dữ liệu
                        _unitOfWork.KoiRepository.Update(koiEntity);
                    }
                }

                // **Xử lý OrderFishes (nếu có)**
                if (order.OrderFishes != null && order.OrderFishes.Any())
                {
                    foreach (var orderFish in order.OrderFishes)
                    {
                        var fishEntity = await _unitOfWork.FishRepository.GetByIdAsync(orderFish.FishesId);
                        if (fishEntity == null)
                        {
                            return NotFound($"Fish with ID = {orderFish.FishesId} not found.");
                        }

                        if (fishEntity.quantityInStock < orderFish.Quantity)
                        {
                            return BadRequest($"Not enough stock for Fish ID = {orderFish.FishesId}. " +
                                              $"Requested: {orderFish.Quantity}, Available: {fishEntity.Quantity}");
                        }

                        // Trừ số lượng Fish trong kho

                        fishEntity.quantityInStock -= orderFish.Quantity;

                        // Tính tổng tiền từ Koi với chuyển đổi rõ ràng
                        totalMoney += fishEntity.Price.GetValueOrDefault(0) * (orderFish.Quantity ?? 0);

                        // Cập nhật vào cơ sở dữ liệu
                        _unitOfWork.FishRepository.Update(fishEntity);
                    }
                }

                // **Gán thông tin cho Order**
                order.TotalMoney = totalMoney;
                order.FinalMoney = totalMoney - (order.DiscountMoney ?? 0);
                order.OrderDate = DateOnly.FromDateTime(DateTime.UtcNow);
                order.OrderStatus = "processing";

                // **Lưu Order vào cơ sở dữ liệu**
                await _unitOfWork.OrderRepository.CreateAsync(order);
                await _unitOfWork.OrderRepository.SaveAsync();



                await transaction.CommitAsync();  // Commit giao dịch nếu thành công.
                return CreatedAtAction(nameof(GetOrders), new { id = order.OrderId }, order);

                // **Trả về thông tin Order đã tạo**
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



       

    }
}