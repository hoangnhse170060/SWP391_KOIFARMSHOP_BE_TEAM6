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
                    address = order.User?.Address,
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
public async Task<ActionResult<object>> CreateOrder([FromBody] Order order)
{
    if (order == null)
    {
        return BadRequest("Order object is null.");
    }

    using var transaction = await _unitOfWork.OrderRepository.BeginTransactionAsync();

    try
    {
        decimal totalMoney = 0;

        // **Pre-Validate OrderKois**
        foreach (var orderKoi in order.OrderKois)
        {
            var koiEntity = await _unitOfWork.KoiRepository.GetByIdAsync(orderKoi.KoiId);
            if (koiEntity == null)
            {
                return NotFound($"Koi with ID = {orderKoi.KoiId} not found.");
            }

            if (koiEntity.quantityInStock < 1)
            {
                return BadRequest($"Koi with ID = {orderKoi.KoiId} is out of stock.");
            }
        }

        // **Pre-Validate OrderFishes**
        foreach (var orderFish in order.OrderFishes)
        {
            var fishEntity = await _unitOfWork.FishRepository.GetByIdAsync(orderFish.FishesId);
            if (fishEntity == null)
            {
                return NotFound($"Fish with ID = {orderFish.FishesId} not found.");
            }

            if (fishEntity.quantityInStock < orderFish.Quantity)
            {
                return BadRequest(
                    $"Not enough stock for Fish ID = {orderFish.FishesId}. Available: {fishEntity.quantityInStock}, Requested: {orderFish.Quantity}");
            }
        }

        // **Process OrderKois**
        foreach (var orderKoi in order.OrderKois)
        {
            var koiEntity = await _unitOfWork.KoiRepository.GetByIdAsync(orderKoi.KoiId);

            // Default Koi quantity to 1
            orderKoi.Quantity = 1;

            // Reduce stock and update status
            koiEntity.quantityInStock -= 1;
            if (koiEntity.quantityInStock == 0)
            {
                koiEntity.Status = "unavailable";
            }

            totalMoney += koiEntity.Price.GetValueOrDefault();
            _unitOfWork.KoiRepository.Update(koiEntity);
        }

        // **Process OrderFishes**
        foreach (var orderFish in order.OrderFishes)
        {
            var fishEntity = await _unitOfWork.FishRepository.GetByIdAsync(orderFish.FishesId);

            // Reduce stock
            fishEntity.quantityInStock -= orderFish.Quantity;

            // Update status if stock is depleted
            if (fishEntity.quantityInStock == 0)
            {
                fishEntity.Status = "unavailable";
            }

            totalMoney += (fishEntity.Price.GetValueOrDefault() * (orderFish.Quantity ?? 0));
            _unitOfWork.FishRepository.Update(fishEntity);
        }

        // **Calculate FinalMoney and Discount**
        decimal discountMoney = order.DiscountMoney ?? 0;
        order.TotalMoney = totalMoney;
        order.FinalMoney = totalMoney - discountMoney;

        if (order.FinalMoney < 0)
        {
            return BadRequest("Final money cannot be less than zero after applying the discount.");
        }

        order.OrderDate = DateOnly.FromDateTime(DateTime.UtcNow);
        order.OrderStatus = "processing";

        // **Save Order to Database**
        await _unitOfWork.OrderRepository.CreateAsync(order);
        await _unitOfWork.OrderRepository.SaveAsync();
        await transaction.CommitAsync();

        // **Call GetOrderById to Get Detailed Data**
        var getOrderResult = await GetOrderById(order.OrderId);

        if (getOrderResult.Result is OkObjectResult okResult)
        {
            return Ok(okResult.Value);
        }

        return getOrderResult.Result;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        _logger.LogError(ex, $"Error creating order: {ex.Message}");
        return StatusCode(500, $"Internal server error: {ex.Message}");
    }
}

        [HttpDelete("{orderId:int}/{itemType}/{itemId:int}")]
        public async Task<IActionResult> DeleteItemFromOrder(int orderId, string itemType, int itemId)
        {
            try
            {
                // Lấy thông tin đơn hàng
                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null)
                {
                    return NotFound(new
                    {
                        message = $"Order with ID = {orderId} not found."
                    });
                }

                // Kiểm tra trạng thái đơn hàng
                if (order.OrderStatus is "completed" or "canceled" or "remittance")
                {
                    return BadRequest(new
                    {
                        message = $"Cannot delete items from an order with status {order.OrderStatus}."
                    });
                }

                decimal amountToDeduct = 0;

                // Xử lý sản phẩm loại "koi"
                if (itemType.ToLower() == "koi")
                {
                    var orderKoi = order.OrderKois.FirstOrDefault(ok => ok.KoiId == itemId);
                    if (orderKoi == null)
                    {
                        return NotFound(new
                        {
                            message = $"Koi with ID = {itemId} not found in the order."
                        });
                    }

                    var koi = await _unitOfWork.KoiRepository.GetByIdAsync(orderKoi.KoiId);
                    if (koi == null)
                    {
                        return NotFound(new
                        {
                            message = $"Koi with ID = {itemId} not found in the database."
                        });
                    }

                    koi.quantityInStock += orderKoi.Quantity.GetValueOrDefault(0);
                    koi.Status = koi.quantityInStock > 0 ? "available" : "unavailable";
                    _unitOfWork.KoiRepository.Update(koi);

                    amountToDeduct = koi.Price.GetValueOrDefault(0) * orderKoi.Quantity.GetValueOrDefault(0);
                    _unitOfWork.OrderKoiRepository.Remove(orderKoi);
                }
      
                else if (itemType.ToLower() == "fish")
                {
                    var orderFish = order.OrderFishes.FirstOrDefault(of => of.FishesId == itemId);
                    if (orderFish == null)
                    {
                        return NotFound(new
                        {
                            message = $"Fish with ID = {itemId} not found in the order."
                        });
                    }

                    var fish = await _unitOfWork.FishRepository.GetByIdAsync(orderFish.FishesId);
                    if (fish == null)
                    {
                        return NotFound(new
                        {
                            message = $"Fish with ID = {itemId} not found in the database."
                        });
                    }

                    fish.quantityInStock += orderFish.Quantity.GetValueOrDefault(0);
                    fish.Status = fish.quantityInStock > 0 ? "available" : "unavailable";
                    _unitOfWork.FishRepository.Update(fish);

                    amountToDeduct = fish.Price.GetValueOrDefault(0) * orderFish.Quantity.GetValueOrDefault(0);
                    _unitOfWork.OrderFishesRepository.Remove(orderFish);
                }
                else
                {
                    return BadRequest(new
                    {
                        message = "Invalid item type. Use 'koi' or 'fish'."
                    });
                }

                order.TotalMoney -= amountToDeduct;
                order.FinalMoney = order.TotalMoney - (order.DiscountMoney ?? 0);

                if (!order.OrderKois.Any() && !order.OrderFishes.Any())
                {
                    _unitOfWork.OrderRepository.Remove(order);
                    await _unitOfWork.SaveAsync();

                    return Ok(new
                    {
                        message = "All items have been removed from the order. The order has been deleted.",
                        orderId = order.OrderId
                    });
                }

                _unitOfWork.OrderRepository.Update(order);
                await _unitOfWork.SaveAsync();

                return Ok(new
                {
                    message = "Item successfully removed from the order.",
                    remainingTotal = order.FinalMoney,
                    orderId = order.OrderId
                });
            }
            catch (DbUpdateConcurrencyException ex)
            {

                var orderExists = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);
                if (orderExists == null)
                {
                    return Ok(new
                    {
                        message = "Order was deleted successfully.",
                        orderId = orderId
                    });
                }

                _logger.LogError(ex, $"Concurrency error occurred while deleting item from order {orderId}.");
                return Conflict(new
                {
                    message =
                        "The record you are trying to update or delete was modified or deleted by another process. Please refresh the data and try again.",
                    orderId = orderId,
                    itemId = itemId,
                    itemType = itemType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting item from order {orderId}.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

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

                if (order.OrderStatus.ToLower() == "remittance")
                {
                    order.OrderStatus = "delivering";
                    order.ShippingDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2));
                }
                else
                {
                    return BadRequest("Only orders with 'remittance' status can be updated to 'delivering'.");
                }

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
                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null)
                {
                    return NotFound($"Order with ID = {orderId} not found.");
                }

                if (order.OrderStatus != "processing" && order.OrderStatus != "remittance")
                {
                    return BadRequest("Only orders in 'processing' or 'remittance' status can be canceled.");
                }

                decimal totalMoney = 0;

                // Restock Koi items and update status
                foreach (var koi in order.OrderKois)
                {
                    var koiEntity = await _unitOfWork.KoiRepository.GetByIdAsync(koi.KoiId);
                    if (koiEntity != null)
                    {
                        // Hoàn trả số lượng tồn kho
                        koiEntity.quantityInStock += koi.Quantity.GetValueOrDefault(0);

                        // Cập nhật trạng thái mặt hàng
                        koiEntity.Status = koiEntity.quantityInStock > 0 ? "available" : "unavailable";
                        _unitOfWork.KoiRepository.Update(koiEntity);
                    }

                    totalMoney += koi.Quantity.GetValueOrDefault(0) * koiEntity.Price.GetValueOrDefault(0);

                    // Đặt lại số lượng trong đơn hàng về 0
                    koi.Quantity = 0;
                    _unitOfWork.OrderKoiRepository.Update(koi);
                }

                // Restock Fish items and update status
                foreach (var fish in order.OrderFishes)
                {
                    var fishEntity = await _unitOfWork.FishRepository.GetByIdAsync(fish.FishesId);
                    if (fishEntity != null)
                    {
                        // Hoàn trả số lượng tồn kho
                        fishEntity.quantityInStock += fish.Quantity.GetValueOrDefault(0);

                        // Cập nhật trạng thái mặt hàng
                        fishEntity.Status = fishEntity.quantityInStock > 0 ? "available" : "unavailable";
                        _unitOfWork.FishRepository.Update(fishEntity);
                    }

                    totalMoney += fish.Quantity.GetValueOrDefault(0) * fishEntity.Price.GetValueOrDefault(0);

                    // Đặt lại số lượng trong đơn hàng về 0
                    fish.Quantity = 0;
                    _unitOfWork.OrderFishesRepository.Update(fish);
                }

                // Cập nhật trạng thái đơn hàng
                order.OrderStatus = "canceled";
                _unitOfWork.OrderRepository.Update(order);

                // Điều chỉnh điểm của người dùng
                var userId = order.UserId.GetValueOrDefault();
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.TotalPoints -= order.EarnedPoints.GetValueOrDefault(0);
                    if (user.TotalPoints < 0) user.TotalPoints = 0;
                    _unitOfWork.UserRepository.Update(user);
                }

                await _unitOfWork.SaveAsync();
                await transaction.CommitAsync();

                _logger.LogInformation(
                    $"Order {orderId} canceled, items restocked, quantities reset to 0, and user points adjusted.");

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
       
                var orders = await _unitOfWork.OrderRepository.GetAllAsync(
                    include: query => query
                        .Include(o => o.User)
                        .Include(o => o.OrderKois).ThenInclude(ok => ok.Koi)
                        .Include(o => o.OrderFishes).ThenInclude(of => of.Fishes)
                        .Include(o => o.Promotion)
                );

                // Chuẩn bị dữ liệu trả về
                var result = new List<object>();

                foreach (var order in orders)
                {
                    // Lấy điểm giao dịch của từng đơn hàng
                    var pointTransactions = await _unitOfWork.PointRepository.GetPointsByOrderIdAsync(order.OrderId);

                    // Format dữ liệu giao dịch điểm
                    var formattedPoints = pointTransactions?.Select(pt => new
                    {
                        transactionId = pt.TransactionId,
                        userId = pt.UserId,
                        transactionType = pt.TransactionType,
                        transactionDate = pt.TransactionDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"), // ISO format
                        pointsChanged = pt.PointsChanged,
                        newTotalPoints = pt.NewTotalPoints,
                        orderId = pt.OrderId
                    }).ToList();

                    // Chuẩn bị dữ liệu cho từng đơn hàng
                    var orderData = new
                    {
                        order.OrderId,
                        order.UserId,
                        username = order.User?.UserName,
                        address = order.User?.Address,
                        promotion = order.Promotion?.PromotionName,
                        order.OrderDate,
                        totalMoney = order.TotalMoney,
                        finalMoney = order.FinalMoney,
                        discountMoney = order.DiscountMoney,
                        usedPoints = order.UsedPoints,
                        earnedPoints = order.EarnedPoints,
                        orderStatus = order.OrderStatus,
                        paymentMethod = order.PaymentMethod,
                        point_transaction = formattedPoints, 

                        orderKois = order.OrderKois.Select(ok => new
                        {
                            ok.KoiId,
                            ok.Quantity,
                            koiDetails = new
                            {
                                ok.Koi.KoiId,
                                ok.Koi.Name,
                                ok.Koi.Gender,
                                ok.Koi.Price,
                                ok.Koi.Size,
                                ok.Koi.ImageKoi
                            }
                        }).ToList(),
                        orderFishes = order.OrderFishes.Select(of => new
                        {
                            of.FishesId,
                            of.Quantity,
                            fishDetails = new
                            {
                                of.Fishes.FishesId,
                                of.Fishes.Name,
                                of.Fishes.Status,
                                of.Fishes.Price,
                                of.Fishes.ImageFishes
                            }
                        }).ToList()
                    };

                    result.Add(orderData);
                }

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
                var order = await _unitOfWork.OrderRepository.GetByIdAsync(orderId);

                if (order == null)
                {
                    return NotFound($"Order with ID = {orderId} not found.");
                }

                if (order.OrderStatus?.ToLower() == "delivering")
                {
                    order.OrderStatus = "completed";

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

                var orders = await _unitOfWork.OrderRepository.GetAllAsync(
                    include: query => query
                        .Include(o => o.User)
                        .Include(o => o.OrderFishes).ThenInclude(f => f.Fishes)
                        .Include(o => o.OrderKois).ThenInclude(k => k.Koi)
                        .Include(o => o.Promotion)
                );

                var userOrders = orders.Where(o => o.UserId == userId).ToList();

                if (!userOrders.Any())
                {
                    return NotFound($"No orders found for User with ID = {userId}.");
                }

                var result = new List<object>();

                foreach (var order in userOrders)
                {
                    
                    var pointTransactions = await _unitOfWork.PointRepository.GetPointsByOrderIdAsync(order.OrderId);

                    var formattedPoints = pointTransactions?.Select(pt => new
                    {
                        transactionId = pt.TransactionId,
                        userId = pt.UserId,
                        transactionType = pt.TransactionType,
                        transactionDate = pt.TransactionDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"), // ISO format
                        pointsChanged = pt.PointsChanged,
                        newTotalPoints = pt.NewTotalPoints,
                        orderId = pt.OrderId
                    }).ToList();

                    var orderData = new
                    {
                        orderId = order.OrderId,
                        userId = order.UserId,
                        userName = order.User?.UserName,
                        email = order.User?.Email,
                        phoneNumber = order.User?.PhoneNumber,
                        address = order.User?.Address,
                        promotion = order.Promotion?.PromotionName,
                        orderDate = order.OrderDate.ToString(),
                        totalMoney = order.TotalMoney,
                        finalMoney = order.FinalMoney,
                        orderStatus = order.OrderStatus,
                        paymentMethod = order.PaymentMethod,
                        earnedPoints = order.EarnedPoints,
                        usedPoints = order.UsedPoints,
                        point_transaction = formattedPoints, 
                        fishes = order.OrderFishes.Select(f => new
                        {
                            fishesId = f.FishesId,
                            quantity = f.Quantity,
                            name = f.Fishes.Name,
                            status = f.Fishes.Status,
                            price = f.Fishes.Price,
                            imageFishes = f.Fishes.ImageFishes
                        }),
                        kois = order.OrderKois.Select(k => new
                        {
                            koiId = k.KoiId,
                            quantity = k.Quantity,
                            name = k.Koi.Name,
                            gender = k.Koi.Gender,
                            price = k.Koi.Price,
                            size = k.Koi.Size,
                            imageKoi = k.Koi.ImageKoi
                        })
                    };

                    result.Add(orderData);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders for the logged-in user.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpGet("status/{status}")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrdersByStatus(string status)
        {
            try
            {
                var orders = await _unitOfWork.OrderRepository.GetAllAsync(
                    include: query => query
                        .Include(o => o.User)
                        .Include(o => o.OrderFishes).ThenInclude(f => f.Fishes)
                        .Include(o => o.OrderKois).ThenInclude(k => k.Koi)
                        .Include(o => o.Promotion)
                );

                // Lọc đơn hàng theo trạng thái
                var filteredOrders = orders
                    .Where(o => o.OrderStatus.Equals(status, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (!filteredOrders.Any())
                {
                    return NotFound($"No orders found with status '{status}'.");
                }

                var result = filteredOrders.Select(order => new
                {
                    order.OrderId,
                    order.UserId,
                    UserName = order.User?.UserName,
                    Email = order.User?.Email,
                    PhoneNumber = order.User?.PhoneNumber,
                    Address = order.User?.Address,
                    Promotion = order.Promotion?.PromotionName,
                    order.OrderDate,
                    order.TotalMoney,
                    order.FinalMoney,
                    order.OrderStatus,
                    order.PaymentMethod,
                    order.EarnedPoints,
                    order.UsedPoints,
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
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving orders with status '{status}'.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
     [HttpGet("my-orders/status/{status}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetOrdersByStatusForLoggedInUser(string status)
        {
            try
            {
                // Retrieve UserId from token
                var userIdClaim = User.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("User ID not found in token.");
                }

                int userId = int.Parse(userIdClaim);

                // Query orders of the user with the related information
                var orders = await _unitOfWork.OrderRepository.GetAllAsync(
                    include: query => query
                        .Include(o => o.User) // Include User information
                        .Include(o => o.OrderFishes).ThenInclude(f => f.Fishes)
                        .Include(o => o.OrderKois).ThenInclude(k => k.Koi)
                        .Include(o => o.Promotion)
                );

                // Filter orders by UserId and status
                var userOrders = orders.Where(o => o.UserId == userId && o.OrderStatus.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();

                if (!userOrders.Any())
                {
                    return NotFound($"No orders found with status '{status}' for your account.");
                }

                // Prepare response data
                var result = new List<object>();

                foreach (var order in userOrders)
                {
                    // Get point transactions for the order
                    var pointTransactions = await _unitOfWork.PointRepository.GetPointsByOrderIdAsync(order.OrderId);

                    // Format point transaction data
                    var formattedPoints = pointTransactions?.Select(pt => new
                    {
                        transactionId = pt.TransactionId,
                        userId = pt.UserId,
                        transactionType = pt.TransactionType,
                        transactionDate = pt.TransactionDate.ToString("yyyy-MM-ddTHH:mm:ss.fff"), // ISO format
                        pointsChanged = pt.PointsChanged,
                        newTotalPoints = pt.NewTotalPoints,
                        orderId = pt.OrderId
                    }).ToList();

                    var orderData = new
                    {
                        orderId = order.OrderId,
                        userId = order.UserId,
                        userName = order.User?.UserName,
                        email = order.User?.Email,
                        phoneNumber = order.User?.PhoneNumber,
                        address = order.User?.Address,
                        promotion = order.Promotion?.PromotionName,
                        orderDate = order.OrderDate.ToString(),
                        totalMoney = order.TotalMoney,
                        finalMoney = order.FinalMoney,
                        orderStatus = order.OrderStatus,
                        paymentMethod = order.PaymentMethod,
                        earnedPoints = order.EarnedPoints,
                        usedPoints = order.UsedPoints,
                        point_transaction = formattedPoints,

                        fishes = order.OrderFishes.Select(f => new
                        {
                            fishesId = f.FishesId,
                            quantity = f.Quantity,
                            name = f.Fishes.Name,
                            status = f.Fishes.Status,
                            price = f.Fishes.Price,
                            imageFishes = f.Fishes.ImageFishes
                        }),
                        kois = order.OrderKois.Select(k => new
                        {
                            koiId = k.KoiId,
                            quantity = k.Quantity,
                            name = k.Koi.Name,
                            gender = k.Koi.Gender,
                            price = k.Koi.Price,
                            size = k.Koi.Size,
                            imageKoi = k.Koi.ImageKoi
                        })
                    };

                    result.Add(orderData);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving orders with status '{status}' for the logged-in user.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }
}



//Order