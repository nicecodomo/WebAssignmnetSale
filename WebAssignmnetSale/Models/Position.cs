using System.ComponentModel.DataAnnotations;

namespace WebAssignmentSale.Models
{
    public class Position
    {
        public int PosId { get; set; }

        [Display(Name = "Position Name")]
        public string PosName { get; set; }

        public string PosPermissions { get; set; }

        // Other properties related to Position, if any

        // Navigation property for the related Employees
    }
}
