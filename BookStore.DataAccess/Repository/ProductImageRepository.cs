using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
    public class ProductImageRepository : Repository<ProductImage> , IProductImageRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public ProductImageRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public void Update(ProductImage productImage)
        {
            _dbContext.ProductImages.Update(productImage);
        }
    }
} 