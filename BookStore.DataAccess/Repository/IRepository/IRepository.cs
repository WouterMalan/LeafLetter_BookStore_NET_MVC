using System.Linq.Expressions;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        T Get(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = false);

        void Add (T entity);

        IEnumerable<T> GetAll(Expression<Func<T, bool>>? filters = null, string? includeProperties = null);

        void Delete(T entity);

        void DeleteRange(IEnumerable<T> entity);
    }
}