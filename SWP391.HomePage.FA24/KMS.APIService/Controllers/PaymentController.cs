using KMG.Repository.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KMS.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "customer")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [HttpPost("pay-consignment")]
        public async Task<IActionResult> PayConsignment(int userId, int consignmentId, string paymentMethod, string shippingAddress, decimal discount)
        {
            var result = await _paymentService.ProcessPaymentAsync(userId, consignmentId, paymentMethod, shippingAddress, discount);

            if (!result)
            {
                return BadRequest("Failed to process payment. The consignment might not be available.");
            }

            return Ok("Payment processed successfully and consignment status updated.");
        }
    }
}
