using KMG.Repository;
using KMG.Repository.Dto;
using KMG.Repository.Models;
using KMS.APIService.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Web;
using System.Text;

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
                    Status = "remittance",
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

                if (!int.TryParse(orderInfo, out int orderId))
                    return BadRequest(new { Message = "Invalid Order ID." });

                var order = await _unitOfWork.OrderRepository.GetOrderWithDetailsAsync(orderId);
                if (order == null)
                    return NotFound(new { Message = "Order not found." });

                var user = order.UserId.HasValue
                    ? await _unitOfWork.UserRepository.GetByIdAsync(order.UserId.Value)
                    : null;
                string userName = user?.UserName ?? "Unknown User";

                bool isSignatureValid = ValidateSignature(
                    Request.QueryString.Value.Substring(1, Request.QueryString.Value.IndexOf("&vnp_SecureHash") - 1),
                    vnp_SecureHash, hashSecret);

                if (isSignatureValid && vnp_ResponseCode == "00")
                {
                    order.OrderStatus = "remittance";
                    _unitOfWork.OrderRepository.Update(order);
                    await _unitOfWork.OrderRepository.SaveAsync();

                    // Lấy email của người dùng và email mặc định của staff
                    string recipientEmail = GetUserEmail(order);
                    string staffEmail = "d.anhdn2008@gmail.com";

                    string emailContent = GenerateOrderDetailsEmailContent(order);

                    // Kiểm tra và gửi email đến cả người dùng và staff
                    if (!string.IsNullOrWhiteSpace(recipientEmail) || !string.IsNullOrWhiteSpace(staffEmail))
                    {
                        try
                        {
                            // Gửi email cho người dùng nếu có
                            if (!string.IsNullOrWhiteSpace(recipientEmail))
                            {
                                await SendEmailAsync(recipientEmail, "Payment Confirmation", emailContent);
                            }

                            // Gửi email cho staff nếu có
                            if (!string.IsNullOrWhiteSpace(staffEmail))
                            {
                                await SendEmailAsync(staffEmail, "Payment Confirmation", emailContent);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending email.");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("User or staff email not found. Skipping email sending.");
                    }

                    return Redirect("http://localhost:5173/success");
                }




                else if (vnp_ResponseCode == "24") // Khách hàng hủy giao dịch
                {
                    // Cập nhật trạng thái đơn hàng thành "Canceled"
                    order.OrderStatus = "canceled";


                    // Khôi phục số lượng sản phẩm về kho
                    foreach (var koi in order.OrderKois)
                    {
                        var koiProduct = await _unitOfWork.KoiRepository.GetByIdAsync(koi.KoiId);
                        if (koiProduct != null)
                        {
                            koiProduct.quantityInStock += koi.Quantity;
                            _unitOfWork.KoiRepository.Update(koiProduct);
                        }

                        koi.Quantity = 0;
                        _unitOfWork.OrderKoiRepository.Update(koi);
                    }

                    foreach (var fish in order.OrderFishes)
                    {
                        var fishProduct = await _unitOfWork.FishRepository.GetByIdAsync(fish.FishesId);
                        if (fishProduct != null)
                        {
                            fishProduct.quantityInStock += fish.Quantity;
                            _unitOfWork.FishRepository.Update(fishProduct);
                        }

                        fish.Quantity = 0;
                        _unitOfWork.OrderFishesRepository.Update(fish);
                    }

                    // Trừ điểm đã được cộng cho khách hàng (nếu có)
                    if (user != null && order.EarnedPoints.HasValue && order.EarnedPoints.Value > 0)
                    {
                        // Chuyển TotalPoints sang kiểu int để tính toán chính xác
                        user.TotalPoints = (byte)Math.Max(0, (int)user.TotalPoints - order.EarnedPoints.Value);
                        _unitOfWork.UserRepository.Update(user);
                        Console.WriteLine($"Points : {order.EarnedPoints}");
                    }


                    // Cập nhật thay đổi trong cơ sở dữ liệu
                    _unitOfWork.OrderRepository.Update(order);
                    await _unitOfWork.OrderRepository.SaveAsync();

                    // Chuyển hướng đến URL hủy giao dịch
                    return Redirect("http://localhost:5173/unsuccess");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming payment.");

                // Chuyển hướng đến URL lỗi chung
                return Redirect("https://www.facebook.com/Pham.Jack.3105");
            }

            // Nếu không khớp với bất kỳ điều kiện nào, trả về lỗi mặc định
            return BadRequest(new { Message = "Unexpected error occurred." });
        }


        private bool ValidateSignature(string rspraw, string inputHash, string secretKey)
        {
            string myChecksum = PayLib.HmacSHA512(secretKey, rspraw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }



        private string GetUserEmail(Order order)
        {
            // Kiểm tra xem đối tượng order và user có tồn tại không
            if (order?.User != null && !string.IsNullOrWhiteSpace(order.User.Email))
            {
                return order.User.Email;
            }
            return null; // Trả về null nếu không có email hợp lệ
        }

        private async Task SendEmailAsync(string toEmailAddress, string subject, string content)
        {
            string fromEmailAddress = "koikeshop.swp@gmail.com";
            string fromEmailDisplayName = "[KOIKESHOP] GỬI HOÁ ĐƠN THANH TOÁN";
            string fromEmailPassword = "mlua qksd vbya vijt";
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            bool enabledSsl = true;

            try
            {
                MailMessage message = new MailMessage
                {
                    From = new MailAddress(fromEmailAddress, fromEmailDisplayName),
                    Subject = subject,
                    Body = content,
                    IsBodyHtml = true
                };
                message.To.Add(new MailAddress(toEmailAddress));

                using (SmtpClient smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(fromEmailAddress, fromEmailPassword);
                    smtpClient.EnableSsl = enabledSsl;
                    await smtpClient.SendMailAsync(message); // Gửi email bất đồng bộ
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email.");
            }
        }
        private string GenerateOrderDetailsEmailContent(Order order)
        {
            var sb = new StringBuilder();

            // Hình ảnh tiêu đề
            sb.AppendLine($@"
    <div style='text-align: center; margin-bottom: 20px;'>
        <img src='https://i.postimg.cc/L4cz55Xt/DALL-E-2024-10-29-16-38-43-A-clean-and-elegant-wide-illustration-featuring-a-vibrant-koi-fish-with.jpg' 
             style='width: 100%; max-width: 1350px; height: auto; display: block; margin: 0 auto;' 
             alt='Koi Image'>
    </div>");

            // Bảng thông tin thanh toán
            sb.AppendLine($@"
    <div style='display: flex; justify-content: center;'>
        <table style='border-collapse: collapse; width: 100%; max-width: 1350px; table-layout: fixed; margin: 0 auto;'>
            <colgroup>
                <col style='width: 50%;'>
                <col style='width: 50%;'>
            </colgroup>
            <tr>
                <th colspan='2' 
                    style='background-color: #f2f2f2; 
                           padding: 15px; 
                           font-size: 22px; 
                           text-align: center; 
                           vertical-align: middle;'>
                    PAYMENT INFORMATION
                </th>
            </tr>
            <tr>
                <th style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>Order ID</th>
                <td style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>{order.OrderId}</td>
            </tr>
            <tr>
                <th style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>Order Date</th>
                <td style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>{order.OrderDate:yyyy-MM-dd}</td>
            </tr>
            <tr>
                <th style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>Payment Method</th>
                <td style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>{order.PaymentMethod}</td>
            </tr>
            <tr>
                <th style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>Total Amount</th>
                <td style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>{order.TotalMoney:C}</td>
            </tr>
            <tr>
                <th style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>Final Amount (after discount)</th>
                <td style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>{order.FinalMoney:C}</td>
            </tr>
            <tr>
                <th style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>Customer Name</th>
                <td style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>{order.User?.UserName ?? "N/A"}</td>
            </tr>
            <tr>
                <th style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>Email</th>
                <td style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>{order.User?.Email ?? "N/A"}</td>
            </tr>");

            foreach (var koi in order.OrderKois)
            {
                sb.AppendLine($@"
            <tr>
                <th style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>Koi Name</th>
                <td style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>{koi.Koi.Name}</td>
            </tr>
            <tr>
                <th style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>Quantity</th>
                <td style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>{koi.Quantity}</td>
            </tr>
            <tr>
                <th style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>Price</th>
                <td style='border: 1px solid #ddd; padding: 10px; font-size: 16px; text-align: left;'>{koi.Koi.Price:C}</td>
            </tr>");
            }
            sb.AppendLine($@"
        </table>
    </div>");

            // Phần liên hệ (Contact Info)
            sb.AppendLine($@"
    <div style='background-color: #f8d7da; padding: 20px; margin-top: 20px; border-radius: 8px; max-width: 1350px; margin: 0 auto;'>
        <h2 style='text-align: center; font-family: Arial, sans-serif; color: #d63384;'>CONTACT INFO</h2>
        <p style='text-align: center; font-size: 16px; color: #d63384;'>
            Mail: <a href='mailto:koikeshop.swp@gmail.com' style='color: #d63384;'>koikeshop.swp@gmail.com</a><br>
            Hotline: 0969896403<br>
            Website: <a href='http://koikeshop' style='text-decoration: none; color: #d63384;'>http://koikeshop</a>
        </p>
    </div>");


            return sb.ToString();
        }



    }
}




// 31/10/2024
//PAYMENT11111