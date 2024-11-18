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
using System.Security.Claims;

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
               var points = await _unitOfWork.PointRepository.GetPointsByOrderIdAsync(id);
                // Chuẩn bị dữ liệu trả về
                var result = new
                {
                    order.OrderId,
                    order.UserId,
                    UserName = order.User?.UserName,
                    Email = order.User?.Email,
                    PhoneNumber = order.User?.PhoneNumber,
                    Address = order.Address?.address,
                    Promotion = order.Promotion?.PromotionName,
                    order.OrderDate,
                    order.TotalMoney,
                    order.FinalMoney,
                    order.OrderStatus,
                    order.PaymentMethod,
                    order.EarnedPoints,
                    order.UsedPoints,
                    Point_transaction = points,
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
        
[HttpPost]
public async Task<ActionResult<Order>> CreateOrder([FromBody] Order order)
{
    if (order == null)
    {
        return BadRequest("Order object is null.");
    }

    // Initialize lists if null to avoid NullReferenceException
    order.OrderFishes ??= new List<OrderFish>();
    order.OrderKois ??= new List<OrderKoi>();

    // Merge items with the same `KoiId` in `OrderKois` and force quantity to 1
    order.OrderKois = order.OrderKois
        .GroupBy(k => k.KoiId)
        .Select(g => new OrderKoi
        {
            KoiId = g.Key,
            Quantity = 1 // Force quantity to be 1
        })
        .ToList();

    // Merge items with the same `FishesId` in `OrderFishes` and force quantity to 1
    order.OrderFishes = order.OrderFishes
        .GroupBy(f => f.FishesId)
        .Select(g => new OrderFish
        {
            FishesId = g.Key,
            Quantity = 1 // Force quantity to be 1
        })
        .ToList();

    using var transaction = await _unitOfWork.OrderRepository.BeginTransactionAsync();

    try
    {
        decimal totalMoney = 0;

        // **Process OrderKois**
        foreach (var orderKoi in order.OrderKois)
        {
            var koiEntity = await _unitOfWork.KoiRepository.GetByIdAsync(orderKoi.KoiId);
            if (koiEntity == null)
            {
                return NotFound($"Koi with ID = {orderKoi.KoiId} not found.");
            }

            if (koiEntity.quantityInStock < 1)
            {
                return BadRequest($"Not enough stock for Koi ID = {orderKoi.KoiId}. Available: {koiEntity.quantityInStock}");
            }

            koiEntity.quantityInStock -= 1; // Deduct 1 since quantity is fixed at 1
            if (koiEntity.quantityInStock == 0)
            {
                koiEntity.Status = "unavailable";
            }

            totalMoney += (koiEntity.Price ?? 0) * 1; // Calculate total
            _unitOfWork.KoiRepository.Update(koiEntity);
        }

        // **Process OrderFishes**
        foreach (var orderFish in order.OrderFishes)
        {
            var fishEntity = await _unitOfWork.FishRepository.GetByIdAsync(orderFish.FishesId);
            if (fishEntity == null)
            {
                return NotFound($"Fish with ID = {orderFish.FishesId} not found.");
            }

            if (fishEntity.quantityInStock < 1)
            {
                return BadRequest($"Not enough stock for Fish ID = {orderFish.FishesId}. Available: {fishEntity.quantityInStock}");
            }

            fishEntity.quantityInStock -= 1; // Deduct 1 since quantity is fixed at 1
            if (fishEntity.quantityInStock == 0)
            {
                fishEntity.Status = "unavailable";
            }

            totalMoney += (fishEntity.Price ?? 0) * 1; // Calculate total
            _unitOfWork.FishRepository.Update(fishEntity);
        }

        // **Assign DiscountMoney and FinalMoney**
        decimal discountMoney = order.DiscountMoney ?? 0; // Default discount is 0 if not provided
        order.DiscountMoney = discountMoney; // Assign DiscountMoney to order
        order.TotalMoney = totalMoney; // Assign TotalMoney
        order.FinalMoney = totalMoney - discountMoney; // Calculate FinalMoney

        // Check if FinalMoney is negative
        if (order.FinalMoney < 0)
        {
            return BadRequest("Final money cannot be less than zero after applying the discount.");
        }

        // Set order date and status
        order.OrderDate = DateOnly.FromDateTime(DateTime.UtcNow);
        order.OrderStatus = "processing";

        // **Save Order to Database**
        await _unitOfWork.OrderRepository.CreateAsync(order);
        await _unitOfWork.OrderRepository.SaveAsync();

        // Commit transaction if successful
        await transaction.CommitAsync();

        // Log for verification
        _logger.LogInformation($"Order {order.OrderId} created successfully. TotalMoney: {order.TotalMoney}, DiscountMoney: {order.DiscountMoney}, FinalMoney: {order.FinalMoney}");

        // Return the created Order details
        return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, order);
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
        _logger.LogError(ex, $"Internal server error: {ex.Message}");
        return StatusCode(500, $"Internal server error: {ex.Message}");
    }
}


        [HttpDelete("{orderId:int}/{itemType}/{itemId:int}")]

        public async Task<IActionResult> DeleteItemFromOrder(int orderId, string itemType, int itemId)
        {
            using var transaction = await _unitOfWork.OrderRepository.BeginTransactionAsync();
            try
            {
                // Lấy đơn hàng và kiểm tra sự tồn tại
                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null)
                {
                    return NotFound($"Order with ID = {orderId} not found.");
                }

                // Kiểm tra nếu đơn hàng đã hoàn tất hoặc bị hủy
                if (order.OrderStatus == "completed" || order.OrderStatus == "canceled" ||
                    order.OrderStatus == "remittance")
                {
                    return BadRequest($"Cannot delete items from a {order.OrderStatus} order.");
                }

                decimal amountToDeduct = 0;

                // Xử lý item là 'koi'
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
                // Xử lý item là 'fish'
                else if (itemType.ToLower() == "fish")
                {
                    var orderFish = await _unitOfWork.OrderFishesRepository
                        .FirstOrDefaultAsync(of => of.OrderId == orderId && of.FishesId == itemId);

                    if (orderFish == null)
                        return NotFound($"Fish with ID = {itemId} not found in the order.");

                    var fish = await _unitOfWork.FishRepository.GetByIdAsync(orderFish.FishesId);
                    fish.quantityInStock += orderFish.Quantity;
                    _unitOfWork.FishRepository.Update(fish);

                    amountToDeduct = fish.Price.GetValueOrDefault(0) * (orderFish.Quantity ?? 0);

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

                // Kiểm tra nếu FinalMoney <= 0 và không còn item nào
                if (order.FinalMoney <= 0 && !order.OrderKois.Any() && !order.OrderFishes.Any())
                {
                    _unitOfWork.OrderRepository.Remove(order); // Xóa đơn hàng nếu không còn sản phẩm
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

        // [HttpPut("{orderId:int}/update")]
        // public async Task<IActionResult> UpdateOrder(
        //     int orderId,
        //     [FromQuery] string? itemType = null,
        //     [FromQuery] int? itemId = null,
        //     [FromQuery] int? newQuantity = null,
        //     [FromQuery] string? newPaymentMethod = null)
        // {
        //     using var transaction = await _unitOfWork.OrderRepository.BeginTransactionAsync();
        //     try
        //     {
        //         // Lấy đơn hàng cùng các chi tiết liên quan
        //         var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(orderId);
        //         if (order == null)
        //         {
        //             return NotFound($"Order with ID = {orderId} not found.");
        //         }
        //
        //         // Kiểm tra trạng thái đơn hàng đã hoàn tất chưa
        //         if (order.OrderStatus == "completed" || order.OrderStatus == "remittance")
        //         {
        //             return BadRequest($"{order.OrderStatus}  cannot be updated.");
        //         }
        //
        //         // Kiểm tra số lượng mới có hợp lệ không (phải lớn hơn 0)
        //         if (newQuantity.HasValue && newQuantity <= 0)
        //         {
        //             return BadRequest("Quantity must be greater than zero.");
        //         }
        //
        //         decimal totalMoneyChange = 0;
        //
        //         // Xử lý cập nhật số lượng sản phẩm
        //         if (!string.IsNullOrEmpty(itemType) && itemId.HasValue && newQuantity.HasValue)
        //         {
        //             if (itemType.ToLower() == "koi")
        //             {
        //                 var orderKoi = order.OrderKois.FirstOrDefault(ok => ok.KoiId == itemId);
        //                 if (orderKoi == null)
        //                     return NotFound($"Koi with ID = {itemId} not found in the order.");
        //
        //                 var koi = await _unitOfWork.KoiRepository.GetByIdAsync(orderKoi.KoiId);
        //
        //                 int quantityChange = (int)(newQuantity.Value - orderKoi.Quantity);
        //
        //                 if (quantityChange > 0 && koi.quantityInStock < quantityChange)
        //                 {
        //                     return BadRequest(
        //                         $"Not enough stock for Koi ID = {orderKoi.KoiId}. Available: {koi.quantityInStock}");
        //                 }
        //
        //                 koi.quantityInStock -= quantityChange;
        //                 totalMoneyChange += koi.Price.GetValueOrDefault(0) * quantityChange;
        //                 orderKoi.Quantity = newQuantity.Value;
        //
        //                 // Cập nhật OrderKoi và Koi trong database
        //                 _unitOfWork.OrderKoiRepository.Update(orderKoi);
        //                 _unitOfWork.KoiRepository.Update(koi);
        //             }
        //             else if (itemType.ToLower() == "fish")
        //             {
        //                 var orderFish = order.OrderFishes.FirstOrDefault(of => of.FishesId == itemId);
        //                 if (orderFish == null)
        //                     return NotFound($"Fish with ID = {itemId} not found in the order.");
        //
        //                 var fish = await _unitOfWork.FishRepository.GetByIdAsync(orderFish.FishesId);
        //
        //                 int quantityChange = (int)(newQuantity.Value - orderFish.Quantity);
        //
        //                 if (quantityChange > 0 && fish.quantityInStock < quantityChange)
        //                 {
        //                     return BadRequest(
        //                         $"Not enough stock for Fish ID = {orderFish.FishesId}. Available: {fish.quantityInStock}");
        //                 }
        //
        //                 fish.quantityInStock -= quantityChange;
        //                 totalMoneyChange += fish.Price.GetValueOrDefault(0) * quantityChange;
        //                 orderFish.Quantity = newQuantity.Value;
        //
        //                 // Cập nhật OrderFish và Fish trong database
        //                 _unitOfWork.OrderFishesRepository.Update(orderFish);
        //                 _unitOfWork.FishRepository.Update(fish);
        //             }
        //             else
        //             {
        //                 return BadRequest("Invalid item type. Use 'koi' or 'fish'.");
        //             }
        //         }
        //
        //         // Cập nhật tổng tiền nếu có thay đổi
        //         if (totalMoneyChange != 0)
        //         {
        //             order.TotalMoney += totalMoneyChange;
        //             order.FinalMoney = order.TotalMoney - (order.DiscountMoney ?? 0);
        //         }
        //
        //         // Cập nhật phương thức thanh toán nếu có
        //         if (!string.IsNullOrEmpty(newPaymentMethod))
        //         {
        //             order.PaymentMethod = newPaymentMethod;
        //         }
        //
        //         // Cập nhật đơn hàng trong database
        //         _unitOfWork.OrderRepository.Update(order);
        //
        //         // Lưu tất cả các thay đổi vào database
        //         await _unitOfWork.SaveAsync();
        //         await transaction.CommitAsync();
        //
        //         return Ok("Order updated successfully.");
        //     }
        //     catch (DbUpdateException dbEx)
        //     {
        //         await transaction.RollbackAsync();
        //         var innerException = dbEx.InnerException != null ? dbEx.InnerException.Message : "No inner exception";
        //         _logger.LogError(dbEx, $"Database update error: {dbEx.Message}, Inner Exception: {innerException}");
        //         return StatusCode(500, $"Database update error: {dbEx.Message}, Inner Exception: {innerException}");
        //     }
        //     catch (Exception ex)
        //     {
        //         await transaction.RollbackAsync();
        //         _logger.LogError(ex, $"An error occurred: {ex.Message}");
        //         return StatusCode(500, $"Internal server error: {ex.Message}");
        //     }
        // }




        [HttpPut("{orderId:int}/update-status-staff&manager")]
        [Authorize(Roles = "staff,manager")]
        public async Task<IActionResult> UpdateStatusDeliversing(int orderId)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);

                if (order == null)
                {
                    return NotFound($"Order with ID = {orderId} not found.");
                }

                if (order.OrderStatus.ToLower() == "canceled")
                {
                    return BadRequest("Order has already been canceled and cannot be updated.");
                }

                // Check if the order status is "remittance" and change to "delivering"
                if (order.OrderStatus.ToLower() == "remittance")
                {
                    order.OrderStatus = "delivering";
                    order.ShippingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
                }
                else
                {
                    return BadRequest("Only orders with 'remittance' status can be updated to 'delivering'.");
                }

                // Update order status and save changes
                _unitOfWork.OrderRepository.Update(order);
                await _unitOfWork.OrderRepository.SaveAsync();

                return Ok(
                    $"Order status updated to {order.OrderStatus}. Shipping date set to {order.ShippingDate?.ToString("yyyy-MM-dd")}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    

    [HttpPut("{orderId:int}/orderstatus-canceled")]
        public async Task<IActionResult> CancelOrderWithRestock(int orderId)
        {
            using var transaction = await _unitOfWork.OrderRepository.BeginTransactionAsync();
            try
            {
                // Retrieve the order with details
                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null)
                {
                    return NotFound($"Order with ID = {orderId} not found.");
                }

                if (order.OrderStatus != "processing" || order.OrderStatus == "remittance")
                {
                    return BadRequest("Only orders in 'processing' status can be canceled.");
                }

                decimal totalMoney = 0;

                // Restock Koi items and set quantity to 0
                foreach (var koi in order.OrderKois)
                {
                    var koiEntity = await _unitOfWork.KoiRepository.GetByIdAsync(koi.KoiId);
                    if (koiEntity != null)
                    {
                        koiEntity.quantityInStock += koi.Quantity.GetValueOrDefault(0); // Restock the item
                        _unitOfWork.KoiRepository.Update(koiEntity); // Update koi entity
                    }

                    totalMoney +=
                        koi.Quantity.GetValueOrDefault(0) * koiEntity.Price.GetValueOrDefault(0); // Add to total money

                    koi.Quantity = 0; // Reset quantity
                    _unitOfWork.OrderKoiRepository.Update(koi); // Update koi in the order
                }

                // Restock Fish items and set quantity to 0
                foreach (var fish in order.OrderFishes)
                {
                    var fishEntity = await _unitOfWork.FishRepository.GetByIdAsync(fish.FishesId);
                    if (fishEntity != null)
                    {
                        fishEntity.quantityInStock += fish.Quantity.GetValueOrDefault(0); // Restock fish
                        _unitOfWork.FishRepository.Update(fishEntity); // Update fish entity
                    }

                    totalMoney +=
                        fish.Quantity.GetValueOrDefault(0) *
                        fishEntity.Price.GetValueOrDefault(0); // Add to total money

                    fish.Quantity = 0; // Reset quantity
                    _unitOfWork.OrderFishesRepository.Update(fish); // Update fish in the order
                }

                // Update order status to 'canceled'
                order.OrderStatus = "canceled";
                _unitOfWork.OrderRepository.Update(order); // Save the updated order

                // Retrieve user and adjust points
                var userId = order.UserId.GetValueOrDefault(); // Convert nullable int to int
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.TotalPoints -= order.EarnedPoints.GetValueOrDefault(0); // Deduct earned points
                    if (user.TotalPoints < 0) user.TotalPoints = 0; // Prevent negative points

                    _unitOfWork.UserRepository.Update(user); // Update user entity
                }

                // Commit the changes
                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    $"Order {orderId} canceled, items restocked, quantities reset to 0, and user points adjusted.");

                // Return the updated order and user information
                return Ok(new
                {
                    Message = $"Order with ID {orderId} has been canceled, items restocked, and values reset to 0.",
                    Order = new
                    {
                        order.OrderId,
                        order.TotalMoney,
                        order.FinalMoney,
                        order.UsedPoints,
                        order.EarnedPoints,
                        order.OrderStatus
                    },
                    User = new
                    {
                        user.UserId,
                        TotalPoints = user.TotalPoints
                    }
                });
            }
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                var innerMessage = dbEx.InnerException?.Message ?? "No inner exception";
                _logger.LogError(dbEx, $"Database update error: {dbEx.Message}, Inner Exception: {innerMessage}");
                return StatusCode(500, $"Database update error: {dbEx.Message}, Inner Exception: {innerMessage}");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Internal server error: {ex.Message}");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet]
        [Authorize(Roles = "staff,manager")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrders()
        {
            try
            {
                // Truy vấn tất cả đơn hàng và bao gồm các thông tin liên quan
                var orders = await _unitOfWork.OrderRepository.GetAllAsync(
                    include: query => query
                        .Include(o => o.User) 
                        .Include(o => o.OrderKois)
                        .ThenInclude(ok => ok.Koi)
                        .Include(o => o.OrderFishes)
                        .ThenInclude(of => of.Fishes)
                        .Include(o => o.Address)
                        .Include(o => o.Promotion)
                        .Include(o => o.Point)
                );
                
                var result = orders.Select(order => new
                {
                    order.OrderId,
                    order.UserId,
                    Username = order.User?.UserName, 
                    Address = order.Address?.address,
                    Promotion = order.Promotion?.PromotionName,
                    
                    order.OrderDate,
                    order.TotalMoney,
                    order.FinalMoney,
                    order.DiscountMoney,
                    order.UsedPoints,
                    order.EarnedPoints,
                    order.OrderStatus,
                    order.PaymentMethod,

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
                _logger.LogError(ex, "Error retrieving orders.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPut("{orderId:int}/update-status-COMPLETED")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId)
        {
            try
            {
                // Retrieve the order from the repository
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);

                if (order == null)
                {
                    return NotFound($"Order with ID = {orderId} not found.");
                }

                // Check if the order status is 'in transit'
                if (order.OrderStatus?.ToLower() == "delivering")
                {
                    // Update delivery status to 'delivered'
                    order.OrderStatus = "completed";
                    // Update the order in the repository
                    _unitOfWork.OrderRepository.Update(order);
                    await _unitOfWork.OrderRepository.SaveAsync();

                    _logger.LogInformation($"Order {orderId} delivery status updated to 'Completed'.");

                    return Ok($"Order {orderId} delivery status successfully updated to 'Completed'.");
                }
                else
                {
                    return BadRequest("Order is not in 'in transit' status, so it cannot be updated to 'Completed'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating delivery status for order {orderId}.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("my-orders")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetOrdersForLoggedInUser()
        {
            try
            {
                // Lấy UserId từ token
                var userIdClaim = User.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("User ID not found in token.");
                }

                int userId = int.Parse(userIdClaim);

                // Truy vấn đơn hàng của người dùng với thông tin liên quan
                var orders = await _unitOfWork.OrderRepository.GetAllAsync(
                    include: query => query
                        .Include(o => o.User) // Bao gồm thông tin User để lấy UserName
                        .Include(o => o.OrderKois).ThenInclude(ok => ok.Koi)
                        .Include(o => o.OrderFishes).ThenInclude(of => of.Fishes)
                        .Include(o => o.Address)
                        .Include(o => o.Promotion)
                );

                // Lọc các đơn hàng của người dùng
                var userOrders = orders.Where(o => o.UserId == userId).ToList();

                if (!userOrders.Any())
                {
                    return NotFound($"No orders found for User with ID = {userId}.");
                }

                // Chuẩn bị dữ liệu trả về
                var result = userOrders.Select(order => new
                {
                    order.OrderId,
                    order.UserId,
                    UserName = order.User?.UserName, 
                    Address = order.Address?.address,
                    Promotion = order.Promotion?.PromotionName,
                    order.OrderDate,
                    order.TotalMoney,
                    order.FinalMoney,
                    order.DiscountMoney,
                    order.UsedPoints,
                    order.EarnedPoints,
                    order.OrderStatus,
                    order.PaymentMethod,

                    OrderKois = order.OrderKois.Select(ok => new
                    {
                        ok.KoiId,
                        ok.Quantity,
                        ok.Koi.Name,
                        ok.Koi.Gender,
                        ok.Koi.Price,
                        ok.Koi.Size,
                        ok.Koi.ImageKoi
                    }).ToList(),
                    OrderFishes = order.OrderFishes.Select(of => new
                    {
                        of.FishesId,
                        of.Quantity,
                        of.Fishes.Name,
                        of.Fishes.Status,
                        of.Fishes.Price,
                        of.Fishes.ImageFishes
                    }).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for the logged-in user.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }

}





//Order