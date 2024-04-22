namespace WebAssignmentSale.Models
{
    public class AddressIdModel
    {
        public List<Province> Provinces { get; set; }
        public List<Amphure> Amphures { get; set; }
        public List<District> Districts { get; set; }
        public AssignmentSale AssignmentSales { get; set; }
        public int CurrentPage { get; set; }
    }
}
