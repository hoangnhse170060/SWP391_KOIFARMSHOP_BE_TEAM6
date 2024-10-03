using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Models
{
    public class Register
    {
        [Required(ErrorMessage ="UserName can't be blank ")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Password can't be blank ")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Confirm Password can't be blank ")]
        [Compare("Password", ErrorMessage = "Confirm Password and Password do not match !!")]
        public string ConfirmPassword { get; set; }
        [Required(ErrorMessage = "Email can't be blank ")]
        [EmailAddress(ErrorMessage ="Email should be in a proper format")]
        [Remote(action:"IsEmailAlreadyRegister",controller:"User", ErrorMessage = "Email is already used")]
        public string Email { get; set; }

    }
}
