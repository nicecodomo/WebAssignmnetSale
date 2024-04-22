using System;
using System.ComponentModel.DataAnnotations;

namespace WebAssignmentSale.Models
{
    public class Employee
    {
        public int EmpId { get; set; }
        public string EmpName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Salary { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; } 
        public string EmpStatus { get; set; }

        [Required(ErrorMessage = "Please enter a username")]
        public string Username { get; set; }
        public string Password { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public string LastUpdateBy { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public int PosId { get; set; }
        public string PosName { get; set; }
        public string CreateByUsername { get; set; }
        public string LastByUsername { get; set; }
        public string PosPermissions { get; set; }


        public Position Position { get; set; }
    }

    

}

