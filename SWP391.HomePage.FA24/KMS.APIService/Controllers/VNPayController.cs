using KMG.Repository;
using KMG.Repository.Dto;
using KMG.Repository.Models;
using KMS.APIService.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VNPayController : ControllerBase
    {
        private readonly ILogger<OrderController> _logger;
        private readonly UnitOfWork _unitOfWork;
        private readonly string url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        private readonly string tmnCode = "QBK46KBK";
        private readonly string hashSecret = "MFKD4UMO9TUCVYTO8WQRQ01ZZA5I1KPD";

        public VNPayController(UnitOfWork unitOfWork, ILogger<OrderController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }
        [HttpPost("Payment")]
        public IActionResult CreatePayment(int orderId)
        {
            try
            {
                var order = _unitOfWork.OrderRepository.GetById(orderId);

                if (order == null)
                {
                    return NotFound(new { Message = "Order not found." });
                }

                // Normalize order status to avoid case-sensitive issues
                string status = order.OrderStatus?.ToLower();

                // Kiểm tra trạng thái đơn hàng để quyết định có cho phép thanh toán không
                if (status == "completed" || status == "canceled" || status == "remittance")
                {
                    return BadRequest(new { Message = "This order cannot be paid as it has already been finalized." });
                }

                if (status != "processing")
                {
                    return BadRequest(new { Message = "This order is not in a valid state for payment." });
                }

                // Kiểm tra giá trị tổng tiền
                if (order.FinalMoney <= 0)
                {
                    return BadRequest(new { Message = "Invalid order amount." });
                }

                string returnUrl = $"{Request.Scheme}://{Request.Host}/api/vnpay/PaymentConfirm";
                string txnRef = Guid.NewGuid().ToString();
                string clientIPAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

                // Chuẩn bị dữ liệu thanh toán cho VNPay
                PayLib pay = new PayLib();
                pay.AddRequestData("vnp_Version", "2.1.0");
                pay.AddRequestData("vnp_Command", "pay");
                pay.AddRequestData("vnp_TmnCode", tmnCode);

                // Nhân tổng tiền với 100 để chuyển sang đơn vị nhỏ nhất (đồng)
                int amount = Convert.ToInt32(order.FinalMoney) * 100;
                pay.AddRequestData("vnp_Amount", amount.ToString());

                pay.AddRequestData("vnp_BankCode", "");
                pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
                pay.AddRequestData("vnp_CurrCode", "VND");
                pay.AddRequestData("vnp_IpAddr", clientIPAddress);
                pay.AddRequestData("vnp_Locale", "vn");
                pay.AddRequestData("vnp_OrderInfo", orderId.ToString());
                pay.AddRequestData("vnp_OrderType", "other");
                pay.AddRequestData("vnp_ReturnUrl", returnUrl);
                pay.AddRequestData("vnp_TxnRef", txnRef);

                string paymentUrl = pay.CreateRequestUrl(url, hashSecret);

                // Tạo giao dịch thanh toán mới
                var paymentTransaction = new PaymentTransaction
                {
                    OrderId = orderId,
                    TxnRef = txnRef,
                    Amount = (decimal)order.FinalMoney,
                    Status = "Remittance",
                    CreatedDate = DateTime.Now
                };

                _unitOfWork.PaymentTransactionRepository.CreateAsync(paymentTransaction);
                _unitOfWork.PaymentTransactionRepository.SaveAsync();

                return Ok(new { PaymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment.");
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }



        [HttpGet("PaymentConfirm")]
            public async Task<IActionResult> PaymentConfirm()
            {
            try
            {
                if (!Request.QueryString.HasValue)
                    return BadRequest(new { Message = "Invalid Request" });

                var json = HttpUtility.ParseQueryString(Request.QueryString.Value);
                string txnRef = json["vnp_TxnRef"];
                string orderInfo = json["vnp_OrderInfo"];
                string vnp_ResponseCode = json["vnp_ResponseCode"];
                string vnp_SecureHash = json["vnp_SecureHash"];
                int orderId = int.Parse(orderInfo);

                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null)
                    return NotFound(new { Message = "Order not found." });

                var user = order.UserId.HasValue
                    ? await _unitOfWork.UserRepository.GetByIdAsync(order.UserId.Value)
                    : null;
                string userName = user?.UserName ?? "Unknown User";

                bool isSignatureValid = ValidateSignature(
                    Request.QueryString.Value.Substring(1, Request.QueryString.Value.IndexOf("&vnp_SecureHash") - 1),
                    vnp_SecureHash,
                    hashSecret
                );

                if (isSignatureValid && vnp_ResponseCode == "00") // Thanh toán thành công
                {
                    order.OrderStatus = "Remittance";  // Cập nhật trạng thái thành "Remittance"
                    _unitOfWork.OrderRepository.Update(order);
                    await _unitOfWork.OrderRepository.SaveAsync();

                    var response = new
                    {
                        Message = "Payment Success",
                        OrderId = order.OrderId,
                        UserName = userName,
                        TotalAmount = order.TotalMoney,
                        PaymentDate = DateTime.Now,
                        FishDetails = order.OrderFishes.Select(f => new
                        {
                            FishId = f.Fishes.FishesId,
                            f.Quantity,
                            f.Fishes.Name,
                            f.Fishes.Price
                        }).ToList(),
                        KoiDetails = order.OrderKois.Select(k => new
                        {
                            KoiId = k.Koi.KoiId,
                            k.Quantity,
                            k.Koi.Name,
                            k.Koi.Price
                        }).ToList()
                    };

                    return Ok(response);
                }
                else // Thanh toán thất bại hoặc hủy
                {
                    using var transaction = await _unitOfWork.OrderRepository.BeginTransactionAsync();
                    try
                    {
                        order.OrderStatus = "Canceled";
                        _unitOfWork.OrderRepository.Update(order);

                        foreach (var koi in order.OrderKois)
                        {
                            var koiProduct = await _unitOfWork.KoiRepository.GetByIdAsync(koi.KoiId);
                            koiProduct.quantityInStock += koi.Quantity;
                            _unitOfWork.KoiRepository.Update(koiProduct);

                            koi.Quantity = 0;
                            _unitOfWork.OrderKoiRepository.Update(koi);
                        }

                        foreach (var fish in order.OrderFishes)
                        {
                            var fishProduct = await _unitOfWork.FishRepository.GetByIdAsync(fish.FishesId);
                            fishProduct.quantityInStock += fish.Quantity;
                            _unitOfWork.FishRepository.Update(fishProduct);

                            fish.Quantity = 0;
                            _unitOfWork.OrderFishesRepository.Update(fish);
                        }

                        order.TotalMoney = 0;
                        order.FinalMoney = 0;
                        _unitOfWork.OrderRepository.Update(order);

                        await _unitOfWork.OrderRepository.SaveAsync();
                        await transaction.CommitAsync();

                        return BadRequest(new
                        {
                            Message = "Payment Failed or Canceled",
                            OrderId = order.OrderId,
                            UserName = userName
                        });
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error restoring stock.");
                        return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment.");
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }


        [HttpGet("GetOrderById/{orderId}")]
        public async Task<IActionResult> GetOrderById(int orderId)
        {
            try
            {
                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(orderId);

                if (order == null)
                {
                    return NotFound(new { Message = "Order not found." });
                }

                var user = order.UserId.HasValue
                    ? await _unitOfWork.UserRepository.GetByIdAsync(order.UserId.Value)
                    : null;
                string userName = user?.UserName ?? "Unknown User";

                var response = new
                {
                    OrderId = order.OrderId,
                    UserName = userName,
                    TotalAmount = order.TotalMoney,
                    FinalAmount = order.FinalMoney,
                    OrderStatus = order.OrderStatus,
                    FishDetails = order.OrderFishes.Select(f => new
                    {
                        FishId = f.Fishes.FishesId,
                        f.Quantity,
                        f.Fishes.Name,
                        f.Fishes.Price
                    }).ToList(),
                    KoiDetails = order.OrderKois.Select(k => new
                    {
                        KoiId = k.Koi.KoiId,
                        k.Quantity,
                        k.Koi.Name,
                        k.Koi.Price
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order by id.");
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }


        private bool ValidateSignature(string rspraw, string inputHash, string secretKey)
        {
            string myChecksum = PayLib.HmacSHA512(secretKey, rspraw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}

//PAYMENT