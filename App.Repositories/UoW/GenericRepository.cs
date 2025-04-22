using Microsoft.EntityFrameworkCore;
using App.Core.Base;
using App.Repositories.Context;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using App.Core.Constants;

namespace App.Repositories.UoW
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        #region DI Constructor
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public GenericRepository(AppDbContext dbContext)
        {
            _context = dbContext;
            _dbSet = _context.Set<T>();
        }
        #endregion

        public IQueryable<T> Entities 
            => _context.Set<T>();

        #region Exist Entities
        public IQueryable<T> ExistEntities()
        {
            if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
                return _dbSet.Where(x => EF.Property<DateTimeOffset?>(x, "DeletedTime") == null);
                
            return _dbSet;
        }

        public async Task<T> GetExistByIdAsync(object id)
        {
            var entity = await _dbSet.FindAsync(id);
            if (entity is BaseEntity baseEntity && baseEntity.DeletedTime.HasValue)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ResponseCodeConstants.NOT_FOUND, 
                    "Entity is deleted");

            return entity ?? throw new ErrorException(
                StatusCodes.Status404NotFound, 
                ResponseCodeConstants.NOT_FOUND, 
                "Entity not found");
        }

        public async Task<IEnumerable<T>> GetAllExistAsync()
            => await ExistEntities().ToListAsync();

        public async Task<IEnumerable<T>> FindAllExistAsync(Expression<Func<T, bool>> predicate)
            => await ExistEntities()
                    .Where(predicate)
                    .ToListAsync();
        #endregion

        public async Task<IList<T>> GetAllAsync()
            => await _dbSet.ToListAsync();

        public async Task<T?> GetByIdAsync(object id)
            => await _dbSet.FindAsync(id);

        public void Insert(T obj)
            => _dbSet.Add(obj);

        public void InsertRange(IEnumerable<T> entities)
            => _dbSet.AddRange(entities);

        public void Update(T obj)
            => _dbSet.Update(obj);

        public void UpdateFields(T entity, params Expression<Func<T, object>>[] properties)
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
                _dbSet.Attach(entity);

            foreach (var property in properties)
            {
                entry.Property(property).IsModified = true;
            }
        }

        public void Delete(T entity)
            => _dbSet.Remove(entity);

        public void DeleteRange(IEnumerable<T> entities)
            => _dbSet.RemoveRange(entities);

        public async Task DeleteByIdAsync(object id)
        {
            T entity = await _dbSet.FindAsync(id)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    "Entity not found");

            _dbSet.Remove(entity);
        }

        public async Task<List<T>> FindAllAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.Where(predicate).ToListAsync();

        public IQueryable<T> GetQueryable()
            => _dbSet.AsQueryable();

        public async Task<T> FindAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.FirstOrDefaultAsync(predicate) 
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ResponseCodeConstants.NOT_FOUND, 
                    "Entity not found");

        public async Task<BasePaginatedList<T>> GetPagging(IQueryable<T> query, int index, int pageSize)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            pageSize = Math.Clamp(pageSize, 1, 100);  
            index = Math.Max(index, 0);

            //query = query.AsNoTracking();
            //int count = await query.CountAsync();
            int count = await query.AsNoTracking().CountAsync();
            int maxIndex = count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize) - 1;
            index = Math.Min(index, maxIndex);

            var items = await query
                .Skip(index * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<T>(items, count, index, pageSize);
        }
    }
}
