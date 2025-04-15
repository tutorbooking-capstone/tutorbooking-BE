using App.Core.Base;
using System.Linq.Expressions;

namespace App.Repositories.UoW
{
    public interface IGenericRepository<T> where T : class
    {
        IQueryable<T> Entities { get; }

        // Exist Entities
        IQueryable<T> ExistEntities();
        Task<T> GetExistByIdAsync(object id);
        Task<IEnumerable<T>> GetAllExistAsync();
        Task<IEnumerable<T>> FindAllExistAsync(Expression<Func<T, bool>> predicate);


        // non async
        IEnumerable<T> GetAll();
        T? GetById(object id);
        void Insert(T obj);
        void InsertRange(IList<T> obj);
        void Update(T obj);
        void Delete(object id);
        void Save();

        Task<IList<T>> GetAllAsync();
        Task<BasePaginatedList<T>> GetPagging(IQueryable<T> query, int index, int pageSize);
        Task<T?> GetByIdAsync(object id);
        Task InsertAsync(T obj);
        Task UpdateAsync(T obj);
        Task DeleteAsync(object id);
        Task SaveAsync();
        Task<T> FindAsync(Expression<Func<T, bool>> predicate);
        Task<List<T>> FindAllAsync(Expression<Func<T, bool>> predicate);
        Task<IQueryable<T>> GetAllQueryableAsync();
        IQueryable<T> GetQueryable();
    }
}
