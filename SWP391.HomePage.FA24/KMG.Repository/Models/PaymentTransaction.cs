using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; } // Khóa chính
        public int OrderId { get; set; } // Khóa ngoại
        public string TxnRef { get; set; } // Mã giao dịch từ VNPay
        public decimal Amount { get; set; } // Số tiền giao dịch
        public string Status { get; set; } // Trạng thái giao dịch
        public DateTime CreatedDate { get; set; } // Ngày tạo giao dịch
    }

}

