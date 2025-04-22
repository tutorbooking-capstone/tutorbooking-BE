using App.Core.Base;
using System.Linq.Expressions;

namespace App.Repositories.UoW
{
    public interface IGenericRepository<T> where T : class
    {
        IQueryable<T> Entities { get; }
        IQueryable<T> GetQueryable();

        // Exist Entities
        IQueryable<T> ExistEntities();
        Task<T> GetExistByIdAsync(object id);
        Task<IEnumerable<T>> GetAllExistAsync();
        Task<IEnumerable<T>> FindAllExistAsync(Expression<Func<T, bool>> predicate);

        //Base CRUD
        void Insert(T obj);
        void InsertRange(IEnumerable<T> entities);
        void Update(T obj);
        void UpdateFields(T entity, params Expression<Func<T, object>>[] properties);
        void Delete(T entity);
        void DeleteRange(IEnumerable<T> entities);
        Task DeleteByIdAsync(object id);

        // Base Query
        Task<IList<T>> GetAllAsync();
        Task<T?> GetByIdAsync(object id);

        Task<T> FindAsync(Expression<Func<T, bool>> predicate);
        Task<List<T>> FindAllAsync(Expression<Func<T, bool>> predicate);
        Task<BasePaginatedList<T>> GetPagging(IQueryable<T> query, int index, int pageSize);

    }
}
