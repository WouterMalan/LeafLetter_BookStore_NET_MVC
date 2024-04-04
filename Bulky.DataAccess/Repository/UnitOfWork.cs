using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess;

public class UnitOfWork : IUnitOfWork
{
    private ApplicationDbContext dbContext;
    public ICategoryRepository Category { get; private set; }
    public IProductRepository Product { get; private set; }

    public ICompanyRepository Company { get; private set; }

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
        Category = new CategoryRepository(dbContext);
        Product = new ProductRepository(dbContext);
        Company = new CompanyRepository(dbContext);
    }

    public void Save()
    {
        dbContext.SaveChanges();
    }
}
