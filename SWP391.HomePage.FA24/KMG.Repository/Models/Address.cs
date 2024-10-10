using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KMG.Repository.Models
{
    public class Address
    {
        [Key]
        public int AddressID { get; set; } 

        [ForeignKey("User")]
        public int UserID { get; set; } 

        [Required]
        [MaxLength(int.MaxValue)]
        public string Street { get; set; }

        [MaxLength(int.MaxValue)] 
        public string City { get; set; } 

        [MaxLength(int.MaxValue)] 
        public string AddressLine { get; set; } 

        [Required]
        [MaxLength(10)] 
        [RegularExpression("home|work|other", ErrorMessage = "Invalid address type. Must be 'home', 'work', or 'other'.")]
        public string AddressType { get; set; } 

        public bool IsDefault { get; set; } = false; 

        
        public virtual User User { get; set; } 
    }
}
