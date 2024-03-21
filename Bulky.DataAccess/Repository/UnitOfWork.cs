using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;

namespace Bulky.DataAccess;

public class UnitOfWork : IUnitOfWork
{
    private ApplicationDbContext dbContext;
    public ICategoryRepository Category { get; private set; }

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        this.dbContext = dbContext;
        Category = new CategoryRepository(dbContext);
    }

    public void Save()
    {
        dbContext.SaveChanges();
    }
}
