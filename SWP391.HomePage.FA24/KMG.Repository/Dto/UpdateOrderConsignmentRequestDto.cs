using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Dto
{
    public class UpdateOrderConsignmentRequestDto
    {

        public decimal ConsignmentPrice { get; set; }
        public string? ConsignmentTitle { get; set; }
        public string? ConsignmentDetail { get; set; }
    }

}
