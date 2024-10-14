namespace ECommerceMVC.ViewModels
{
    public class VnPaymentRequestModel
    {
        public string FullName { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedDate { get; set; }
        public string OrderId { get; set; }
    }

    public class VnPaymentResponseModel
    {
      

        public bool Success { get; set; }
        public string PaymentMethod { get; set; }
        public string OrderDescription { get; set; }
        public string OrderId { get; set; }
        public string TransactionId { get; set; }
        public string Token { get; set; }
        public string VnPayResponseCode { get; set; }
    }
}
