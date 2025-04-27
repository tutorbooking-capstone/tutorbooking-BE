using App.Core.Base;
using App.Core.Provider;
using System.Linq.Expressions;
using System.Threading;

namespace App.Repositories.UoW
{
    public interface IGenericRepository<T> where T : class
    {
        ICurrentUserProvider CurrentUserProvider { get; }
        IQueryable<T> Entities { get; }
        IQueryable<T> GetQueryable();

        // Exist Entities
        IQueryable<T> ExistEntities();
        Task<T> GetExistByIdAsync(object id, CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> GetAllExistAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<T>> FindAllExistAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        //Base CRUD
        void Insert(T obj);
        void InsertRange(IEnumerable<T> entities);
        void Update(T obj);
        void UpdateFields(T entity, params Expression<Func<T, object>>[] properties);
        void Delete(T entity, bool isSoftDelete = true);
        void DeleteRange(IEnumerable<T> entities, bool isSoftDelete = true);
        Task DeleteByIdAsync(object id, bool isSoftDelete = true, CancellationToken cancellationToken = default);

        // Base Query
        Task<IList<T>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

        Task<T> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<List<T>> FindAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
        Task<BasePaginatedList<T>> GetPagging(IQueryable<T> query, int index, int pageSize, CancellationToken cancellationToken = default);
    }
}
