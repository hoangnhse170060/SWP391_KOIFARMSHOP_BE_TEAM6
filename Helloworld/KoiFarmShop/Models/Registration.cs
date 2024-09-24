using System.Data;

namespace KoiFarmShop.Models
{
    public class Registration
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public int IsActive { get; set; }

    }
}
