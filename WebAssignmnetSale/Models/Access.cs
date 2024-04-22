using WebAssignmentSale.Models;

namespace WebAssignmentSale.Models
{
    public class Access
    {
        public int AccId { get; set; }
        public string AccStatus { get; set; }

        public int LogId { get; set; }
        public int PgId { get; set; }
       
        public Login Logins { get; set; }
    }
}
