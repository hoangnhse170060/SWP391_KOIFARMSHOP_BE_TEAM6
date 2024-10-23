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
using Microsoft.AspNetCore.Authorization;

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

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetOrders()
        {
            try
            {
                // Truy vấn tất cả đơn hàng và bao gồm các thông tin liên quan
                var orders = await _unitOfWork.OrderRepository.GetAllAsync(
                    include: query => query
                        .Include(o => o.OrderKois)
                            .ThenInclude(ok => ok.Koi)
                        .Include(o => o.OrderFishes)
                            .ThenInclude(of => of.Fishes)
                );

                // Chuẩn bị dữ liệu trả về (bỏ feedbacks và purchaseHistories)
                var result = orders.Select(order => new
                {
                    order.OrderId,
                    order.UserId,
                    order.OrderDate,
                    order.TotalMoney,
                    order.FinalMoney,
                    order.DiscountMoney,
                    order.UsedPoints,
                    order.EarnedPoints,
                    order.OrderStatus,
                    order.PaymentMethod,
                    order.DeliveryStatus,
                    OrderKois = order.OrderKois.Select(ok => new
                    {
                        ok.KoiId,
                        ok.Quantity,
                        KoiDetails = new
                        {
                            ok.Koi.KoiId,
                            ok.Koi.Name,
                            ok.Koi.Gender,
                            ok.Koi.Price,
                            ok.Koi.Size,
                            ok.Koi.ImageKoi
                        }
                    }).ToList(),
                    OrderFishes = order.OrderFishes.Select(of => new
                    {
                        of.FishesId,
                        of.Quantity,
                        FishDetails = new
                        {
                            of.Fishes.FishesId,
                            of.Fishes.Name,
                            of.Fishes.Status,
                            of.Fishes.Price,
                            of.Fishes.ImageFishes
                        }
                    }).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all orders.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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



        [HttpDelete("{orderId:int}/{itemType}/{itemId:int}")]
        public async Task<IActionResult> DeleteItemFromOrder(int orderId, string itemType, int itemId)
        {
            using var transaction = await _unitOfWork.OrderRepository.BeginTransactionAsync();
            try
            {
                
                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null)
                {
                    return NotFound($"Order with ID = {orderId} not found.");
                }

                if (order.OrderStatus == "completed")
                {
                    return BadRequest("Cannot delete items from a completed order.");
                }

                decimal amountToDeduct = 0;



                if (itemType.ToLower() == "koi")
                {
                    var orderKoi = await _unitOfWork.OrderKoiRepository
                        .FirstOrDefaultAsync(ok => ok.OrderId == orderId && ok.KoiId == itemId);

                    if (orderKoi == null)
                        return NotFound($"Koi with ID = {itemId} not found in the order.");

   
                    var koi = await _unitOfWork.KoiRepository.GetByIdAsync(orderKoi.KoiId);
                    koi.quantityInStock += orderKoi.Quantity;
                    _unitOfWork.KoiRepository.Update(koi);


                    amountToDeduct = koi.Price.GetValueOrDefault(0) * (orderKoi.Quantity ?? 0);


                    _unitOfWork.OrderKoiRepository.Remove(orderKoi);
                }
                else if (itemType.ToLower() == "fish")
                {
                    var orderFish = await _unitOfWork.OrderFishesRepository
                        .FirstOrDefaultAsync(of => of.OrderId == orderId && of.FishesId == itemId);

                    if (orderFish == null)
                        return NotFound($"Fish with ID = {itemId} not found in the order.");

                    // Cập nhật số lượng trong kho
                    var fish = await _unitOfWork.FishRepository.GetByIdAsync(orderFish.FishesId);
                    fish.quantityInStock += orderFish.Quantity;
                    _unitOfWork.FishRepository.Update(fish);

                    // Tính tiền cần trừ
                    amountToDeduct = fish.Price.GetValueOrDefault(0) * (orderFish.Quantity ?? 0);

                    // Xóa item Fish khỏi đơn hàng
                    _unitOfWork.OrderFishesRepository.Remove(orderFish);
                }
                else
                {
                    return BadRequest("Invalid item type. Use 'koi' or 'fish'.");
                }

                // Cập nhật tổng tiền của đơn hàng
                order.TotalMoney -= amountToDeduct;
                order.FinalMoney = order.TotalMoney - (order.DiscountMoney ?? 0);
                _unitOfWork.OrderRepository.Update(order);

                // Kiểm tra nếu FinalMoney <= 0 và không còn item nào trong đơn hàng
                if (order.FinalMoney <= 0 && !order.OrderKois.Any() && !order.OrderFishes.Any())
                {
                    // Xóa luôn Order nếu không còn sản phẩm
                    _unitOfWork.OrderRepository.Remove(order);
                }

                // Lưu thay đổi vào cơ sở dữ liệu
                await _unitOfWork.OrderRepository.SaveAsync();
                await transaction.CommitAsync();

                return NoContent(); // Thành công
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                var innerException = dbEx.InnerException != null ? dbEx.InnerException.Message : "No inner exception";
                _logger.LogError(dbEx, $"Database update error: {dbEx.Message}, Inner Exception: {innerException}");
                return StatusCode(500, $"Database update error: {dbEx.Message}, Inner Exception: {innerException}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"An error occurred: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPut("{orderId:int}/update")]
        public async Task<IActionResult> UpdateOrder(
       int orderId,
       [FromQuery] string? itemType = null,
       [FromQuery] int? itemId = null,
       [FromQuery] int? newQuantity = null,
       [FromQuery] string? newPaymentMethod = null)
        {
            using var transaction = await _unitOfWork.OrderRepository.BeginTransactionAsync();
            try
            {
                // Retrieve the order
                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null)
                {
                    return NotFound($"Order with ID = {orderId} not found.");
                }

                // Ensure the order is not completed
                if (order.OrderStatus == "completed")
                {
                    return BadRequest("Completed orders cannot be updated.");
                }

                // Ensure the new quantity is valid (greater than 0)
                if (newQuantity.HasValue && newQuantity <= 0)
                {
                    return BadRequest("Quantity must be greater than zero.");
                }

                decimal totalMoneyChange = 0;

                // Handle item quantity updates
                if (!string.IsNullOrEmpty(itemType) && itemId.HasValue && newQuantity.HasValue)
                {
                    if (itemType.ToLower() == "koi")
                    {
                        var orderKoi = order.OrderKois.FirstOrDefault(ok => ok.KoiId == itemId);
                        if (orderKoi == null)
                            return NotFound($"Koi with ID = {itemId} not found in the order.");

                        var koi = await _unitOfWork.KoiRepository.GetByIdAsync(orderKoi.KoiId);

                        int quantityChange = (int)(newQuantity.Value - orderKoi.Quantity);

                        if (quantityChange > 0 && koi.quantityInStock < quantityChange)
                        {
                            return BadRequest($"Not enough stock for Koi ID = {orderKoi.KoiId}. Available: {koi.quantityInStock}");
                        }

                        koi.quantityInStock -= quantityChange;
                        totalMoneyChange += koi.Price.GetValueOrDefault(0) * quantityChange;
                        orderKoi.Quantity = newQuantity.Value;
                        _unitOfWork.OrderKoiRepository.Update(orderKoi);
                    }
                    else if (itemType.ToLower() == "fish")
                    {
                        var orderFish = order.OrderFishes.FirstOrDefault(of => of.FishesId == itemId);
                        if (orderFish == null)
                            return NotFound($"Fish with ID = {itemId} not found in the order.");

                        var fish = await _unitOfWork.FishRepository.GetByIdAsync(orderFish.FishesId);

                        int quantityChange = (int)(newQuantity.Value - orderFish.Quantity);

                        if (quantityChange > 0 && fish.quantityInStock < quantityChange)
                        {
                            return BadRequest($"Not enough stock for Fish ID = {orderFish.FishesId}. Available: {fish.quantityInStock}");
                        }

                        fish.quantityInStock -= quantityChange;
                        totalMoneyChange += fish.Price.GetValueOrDefault(0) * quantityChange;
                        orderFish.Quantity = newQuantity.Value;
                        _unitOfWork.FishRepository.Update(fish);
                    }
                    else
                    {
                        return BadRequest("Invalid item type. Use 'koi' or 'fish'.");
                    }
                }

                // Update the order total if necessary
                if (totalMoneyChange != 0)
                {
                    order.TotalMoney += totalMoneyChange;
                    order.FinalMoney = order.TotalMoney - (order.DiscountMoney ?? 0);
                }

                // Update payment method if provided
                if (!string.IsNullOrEmpty(newPaymentMethod))
                {
                    order.PaymentMethod = newPaymentMethod;
                }

                _unitOfWork.OrderRepository.Update(order);

                // Save changes to the database
                await _unitOfWork.OrderRepository.SaveAsync();
                await transaction.CommitAsync();

                return Ok("Order updated successfully.");
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                var innerException = dbEx.InnerException != null ? dbEx.InnerException.Message : "No inner exception";
                _logger.LogError(dbEx, $"Database update error: {dbEx.Message}, Inner Exception: {innerException}");
                return StatusCode(500, $"Database update error: {dbEx.Message}, Inner Exception: {innerException}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"An error occurred: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize(Policy = "RequireStaffOrAdmin")]
        [HttpPut("{orderId:int}/update-status")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, [FromQuery] string newStatus)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound($"Order with ID = {orderId} not found.");
                }

                if (order.OrderStatus == "completed")
                {
                    return BadRequest("Order is already completed.");
                }

                if (newStatus.ToLower() == "completed")
                {
                    order.OrderStatus = "completed";
                    order.DeliveryStatus = "in transit";
                }
                else if (newStatus.ToLower() == "canceled")
                {
                    order.OrderStatus = "canceled";
                }
                else
                {
                    return BadRequest("Invalid status.");
                }

                _unitOfWork.OrderRepository.Update(order);
                await _unitOfWork.OrderRepository.SaveAsync();

                return Ok($"Order status updated to {order.OrderStatus}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [Authorize(Policy = "RequireCustomer")]
        [HttpPut("{orderId:int}/cancel-order")]
        public async Task<IActionResult> CancelOrder(int orderId)
        {
            try
            {
                // Lấy đơn hàng từ cơ sở dữ liệu
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    return NotFound($"Order with ID = {orderId} not found.");
                }

                // Kiểm tra nếu đơn hàng đã ở trạng thái "completed"
                if (order.OrderStatus == "completed")
                {
                    return BadRequest("Completed orders cannot be cancelled.");
                }

                // Kiểm tra nếu đơn hàng đang ở trạng thái "processing"
                if (order.OrderStatus == "processing")
                {
                    order.OrderStatus = "cancelled"; // Cập nhật trạng thái thành 'cancelled'

                    _unitOfWork.OrderRepository.Update(order);
                    await _unitOfWork.OrderRepository.SaveAsync();

                    return Ok($"Order with ID {orderId} has been cancelled.");
                }

                // Nếu trạng thái không phải 'processing'
                return BadRequest("Only orders in 'processing' status can be cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




    }
}