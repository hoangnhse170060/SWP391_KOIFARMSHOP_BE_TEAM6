using System.Threading.Tasks;

namespace KMG.Repository.Interfaces
{
    public interface IPaymentService
    {
        Task<bool> ProcessPaymentAsync(int userId, int consignmentId, string paymentMethod, string shippingAddress, decimal discount);
    }
}
