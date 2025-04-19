using Bulky.Models;

namespace Bulky.Models.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<Product> Products { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public SearchFilters SearchFilters { get; set; }
    }

    public class SearchFilters
    {
        public string SearchTerm { get; set; }
        public string Category { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}