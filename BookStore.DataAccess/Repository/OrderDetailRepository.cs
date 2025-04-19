using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public OrderDetailRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }
        
        public void Update(OrderDetail obj)
        {
            _dbContext.OrderDetails.Update(obj);
        }
        
        public void AddRange(IEnumerable<OrderDetail> entities)
        {
            _dbContext.OrderDetails.AddRange(entities);
        }
    }
}