namespace WebAssignmentSale.Models
{
    public class AssignSaleCreateBy
    {
        public int AssignSaleId { get; set; }
        public string Agency { get; set; }
        public string District { get; set; }
        public string Province { get; set; }
        public string Customer { get; set; }
        public string Department { get; set; }
        public string PhoneNumber { get; set; }
        public string LineId { get; set; }
        public string Location { get; set; }
        public string Annotation { get; set; }
        public string AssStatus { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string CreateBy { get; set; }
        public DateTime CreateDate { get; set; }
        public string LastUpdateBy { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public int EmpId { get; set; }
        public string EmpName { get; set; }
        public string CreateByUsername { get; set; }
        public string LastByUsername { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int PosId { get; set; }



        public Employee Employee { get; set; }
    }
}
