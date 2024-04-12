using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
    public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
    {
         private ApplicationDbContext dbContext;

        public ShoppingCartRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            this.dbContext = dbContext;
        }

        public void Update(ShoppingCart product)
        {
            var objFromDb = dbContext.ShoppingCarts.FirstOrDefault(s => s.Id == product.Id);

            if (objFromDb != null)
            {
                objFromDb.ProductId = product.ProductId;
                objFromDb.Count = product.Count;
                objFromDb.ApplicationUserId = product.ApplicationUserId;
            }
        }
    }
}