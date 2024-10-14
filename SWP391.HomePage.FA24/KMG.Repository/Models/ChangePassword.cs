using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Models
{
    public class ChangePassword
    {
        [Required(ErrorMessage = "UserName can't be blank ")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Khom nhap pass doi kieu gi em oi ")]
        public string NewPassword { get; set; }
        [Required(ErrorMessage = "Confirm Password can't be blank ")]
        [Compare("NewPassword", ErrorMessage = "Confirm Password and Password do not match !!")]
        public string ConfirmPassword { get; set; }
    }
}
