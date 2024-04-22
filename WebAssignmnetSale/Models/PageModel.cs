namespace WebAssignmentSale.Models
{
    public class PageModel
    {
        public IEnumerable<AssignmentSale> Items { get; set; }
        public int PageCount { get; set; }
        public int CurrentPage { get; set; }
        public int ItemsPerPage { get; set; }
        public int TotalItems { get; set; }
        public int OrderBy { get; set; }
    }
}
