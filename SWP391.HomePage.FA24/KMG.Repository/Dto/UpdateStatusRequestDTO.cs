using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Dto
{
    public class UpdateStatusRequest
    {
        public int ConsignmentId { get; set; }
        public string Status { get; set; }
    }
}
