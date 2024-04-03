using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;

namespace Bulky.DataAccess.Repository
{
    public class CategoryRepository : Repository<Category> , ICategoryRepository
    {

        private ApplicationDbContext dbContext;

        public CategoryRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            this.dbContext = dbContext;
        }

        public void Update(Category category)
        {
            dbContext.Categories.Update(category);
        }
    }
} 