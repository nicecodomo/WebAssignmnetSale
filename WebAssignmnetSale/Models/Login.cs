using System.ComponentModel.DataAnnotations;
using WebAssignmentSale.Models;

namespace WebAssignmentSale.Models
{
    public class Login
    {
        public int LogId { get; set; }
        [Required(ErrorMessage = "Please enter your username.")]
        public string Username { get; set; }
        [Required(ErrorMessage = "Please enter your password.")]
        public string Password { get; set; }
        public string EmpName { get; set; }
        public int EmpId { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PosName { get; set; }
        public string DepName { get; set; }
        public string PosPermissions { get; set; }

        public Employee Employee { get; set; }
        public Position Position { get; set; }
        public Department Department { get; set; }
    }
}
