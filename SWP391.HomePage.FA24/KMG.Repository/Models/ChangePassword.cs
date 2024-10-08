using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Models
{
    public class ChangePassword
    {
        public string UserName { get; set; }
        public string NewPassword { get; set; }
    }
}
