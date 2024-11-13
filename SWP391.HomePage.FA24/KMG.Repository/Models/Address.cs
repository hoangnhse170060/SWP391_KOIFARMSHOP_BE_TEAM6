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



        [MaxLength(int.MaxValue)]
        public string address { get; set; }

        [Required]
        [MaxLength(10)]
        public string AddressType { get; set; }

        public bool IsDefault { get; set; } = false;


        public virtual User User { get; set; }
    }
}
