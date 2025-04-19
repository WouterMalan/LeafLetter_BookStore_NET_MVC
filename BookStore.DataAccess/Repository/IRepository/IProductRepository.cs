using Bulky.Models;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        void Update(Product product);
        
        Task<IEnumerable<Product>> SearchProducts(string searchTerm, string category, decimal? minPrice, decimal? maxPrice);
    }
}