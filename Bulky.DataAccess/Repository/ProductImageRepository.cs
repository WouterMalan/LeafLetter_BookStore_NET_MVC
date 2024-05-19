using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
    public class ProductImageRepository : Repository<ProductImage> , IProductImageRepository
    {
        private ApplicationDbContext dbContext;

        public ProductImageRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            this.dbContext = dbContext;
        }

        public void Update(ProductImage productImage)
        {
            dbContext.ProductImages.Update(productImage);
        }
    }
} 