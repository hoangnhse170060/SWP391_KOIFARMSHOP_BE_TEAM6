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



                if (isSignatureValid && vnp_ResponseCode == "00") // Thanh toán thành công
                {
                    order.OrderStatus = "remittance";
                    _unitOfWork.OrderRepository.Update(order);
                    await _unitOfWork.OrderRepository.SaveAsync();

                    //// Địa chỉ email cố định để nhận thư khi thanh toán thành công
                    //string recipientEmail = "hoangnh09022003@gmail.com";
                    string recipientEmail = order.User?.Email ?? "default_email@gmail.com";

                    // Generate email content dynamically
                    string emailContent = GenerateOrderDetailsEmailContent(order);

                    // Gửi email tới địa chỉ cố định
                    SendEmail(recipientEmail, "Xác nhận thanh toán thành công", emailContent);

                    // Chuyển hướng đến URL thành công
                    return Redirect("https://www.facebook.com/profile.php?id=100079469285890");
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
                    }


                    // Cập nhật thay đổi trong cơ sở dữ liệu
                    _unitOfWork.OrderRepository.Update(order);
                    await _unitOfWork.OrderRepository.SaveAsync();

                    // Chuyển hướng đến URL hủy giao dịch
                    return Redirect("https://www.facebook.com/page.ngoctrinh");
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


        [HttpPost("CashPayment")]
        public IActionResult CreateCashPayment(int orderId, decimal amountPaid)
        {
            try
            {
                var order = _unitOfWork.OrderRepository.GetById(orderId);

                if (order == null)
                    return NotFound(new { Message = "Order not found." });

                // Kiểm tra phương thức thanh toán
                if (order.PaymentMethod != "Cash")
                    return BadRequest(new { Message = "This order is not allowed for cash payment." });

                // Kiểm tra trạng thái đơn hàng
                if (order.OrderStatus?.ToLower() != "processing")
                    return BadRequest(new { Message = "This order is not in a valid state for payment." });

                // Kiểm tra số tiền đã thanh toán
                if (amountPaid < order.FinalMoney)
                {
                    return BadRequest(new
                    {
                        Message = "The amount paid is less than the required amount. Please enter the correct amount.",
                        RequiredAmount = order.FinalMoney,
                        AmountPaid = amountPaid
                    });
                }
                else if (amountPaid > order.FinalMoney)
                {
                    return BadRequest(new
                    {
                        Message = "The amount paid is more than the required amount. Please enter the exact amount.",
                        RequiredAmount = order.FinalMoney,
                        AmountPaid = amountPaid
                    });
                }



                // Nếu số tiền đã thanh toán khớp với FinalMoney
                order.OrderStatus = "completed";
                _unitOfWork.OrderRepository.Update(order);
                _unitOfWork.OrderRepository.SaveAsync();




                return Ok(new
                {
                    Message = "Payment completed successfully.",
                    OrderId = order.OrderId,
                    AmountPaid = amountPaid,
                    Status = order.OrderStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cash payment.");
                return StatusCode(500, new { Message = "Internal Server Error", Error = ex.Message });
            }
        }


        private bool ValidateSignature(string rspraw, string inputHash, string secretKey)
        {
            string myChecksum = PayLib.HmacSHA512(secretKey, rspraw);
            return myChecksum.Equals(inputHash, StringComparison.InvariantCultureIgnoreCase);
        }

        private void SendEmail(string toEmailAddress, string subject, string content)
        {
            string fromEmailAddress = "koikeshop.swp@gmail.com";
            string fromEmailDisplayName = "[KOIKESHOP] GỬI HOÁ ĐƠN THANH TOÁN ";
            string fromEmailPassword = "maya xeqp vvdf fsnx"; // Sử dụng mật khẩu ứng dụng tại đây
            string smtpHost = "smtp.gmail.com";
            int smtpPort = 587;
            bool enabledSsl = true;

            try
            {
                // Tạo đối tượng MailMessage
                MailMessage message = new MailMessage();
                message.From = new MailAddress(fromEmailAddress, fromEmailDisplayName);
                message.To.Add(new MailAddress(toEmailAddress));
                message.Subject = subject;
                message.Body = content;
                message.IsBodyHtml = true;

                // Cấu hình SMTP client
                using (SmtpClient smtpClient = new SmtpClient(smtpHost, smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(fromEmailAddress, fromEmailPassword);
                    smtpClient.EnableSsl = enabledSsl;

                    // Gửi email
                    smtpClient.Send(message);
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


            sb.AppendLine($@"
    <div style='text-align: center; margin-bottom: 20px;'>
        <img src='https://i.postimg.cc/vmK8B0Fq/Koi-Ke-Zalo-Cover-320x180.png' 
             style='width: 1348px; height: 352px;' 
             alt='Koi Image'>
    </div>");

            // Bảng thông tin đơn hàng và khách hàng
            sb.AppendLine($@"
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
            <th colspan='2' style='background-color: #f2f2f2; padding: 15px; font-size: 22px; text-align: left;'>PAYMENT INFORMATION</th>
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

            sb.AppendLine("</table>");
            sb.AppendLine($@"
    <div style='background-color: #f8d7da; padding: 20px; margin-top: 20px; border-radius: 8px;'>
        <h2 style='text-align: center; font-family: Arial, sans-serif; color: #d63384;'>CONTACT INFO</h2>
        <p style='text-align: center; font-size: 16px; color: #d63384;'>
            Mail: <a href='mailto:koikeshop.swp@gmail.com' style='color: #d63384;'>koikeshop.swp@gmail.com</a><br>
            Holine: 0969896403<br>
            Website: <a href='http://koikeshop' style='text-decoration: none; color: #d63384;'>http://koikeshop</a>
        </p>
    </div>");

            return sb.ToString();
        }



    }

}


//PAYMENT