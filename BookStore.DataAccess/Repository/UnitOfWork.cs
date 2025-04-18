using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    
    public ICategoryRepository Category { get; private set; }
    
    public IProductRepository Product { get; private set; }

    public ICompanyRepository Company { get; private set; }

    public IShoppingCartRepository ShoppingCart { get; private set; }

    public IApplicationUserRepository ApplicationUser { get; private set; }

    public IOrderHeaderRepository OrderHeader { get; private set; }

    public IOrderDetailRepository OrderDetail { get; private set; }

    public IProductImageRepository ProductImage { get; private set; }

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
        ApplicationUser = new ApplicationUserRepository(dbContext);
        Category = new CategoryRepository(dbContext);
        Product = new ProductRepository(dbContext);
        Company = new CompanyRepository(dbContext);
        ShoppingCart = new ShoppingCartRepository(dbContext);
        OrderHeader = new OrderHeaderRepository(dbContext);
        OrderDetail = new OrderDetailRepository(dbContext);
        ProductImage = new ProductImageRepository(dbContext);
    }

    public void Save()
    {
        _dbContext.SaveChanges();
    }
}
