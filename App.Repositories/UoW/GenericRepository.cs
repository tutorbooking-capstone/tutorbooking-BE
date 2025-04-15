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
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;
        public GenericRepository(AppDbContext dbContext)
        {
            _context = dbContext;
            _dbSet = _context.Set<T>();
        }
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

        public void Delete(object id)
        {
            T entity = _dbSet.Find(id) ?? throw new Exception();
            _dbSet.Remove(entity);
        }

        public async Task DeleteAsync(object id)
        {
            T entity = await _dbSet.FindAsync(id) ?? throw new Exception();
            _dbSet.Remove(entity);
        }
        
        public async Task<IList<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public IEnumerable<T> GetAll()
        {
            return _dbSet.AsEnumerable();
        }

        public T? GetById(object id)
        {
            return _dbSet.Find(id);
        }

        public async Task<T?> GetByIdAsync(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<BasePaginatedList<T>> GetPagging(IQueryable<T> query, int index, int pageSize)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);  
            index = Math.Max(index, 0); 
            
            query = query.AsNoTracking();
            int count = await query.CountAsync();
            
            int maxIndex = count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize) - 1;
            index = Math.Min(index, maxIndex);

            var items = await query
                .Skip(index * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new BasePaginatedList<T>(items, count, index, pageSize);
        }

        public void Insert(T obj)
        {
            _dbSet.Add(obj);
        }

        public async Task InsertAsync(T obj)
        {
            await _dbSet.AddAsync(obj);
        }

        public void InsertRange(IList<T> obj)
        {
            _dbSet.AddRange(obj);
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public void Update(T obj)
        {
            _dbSet.Entry(obj).State = EntityState.Modified;
        }

        public Task UpdateAsync(T obj)
        {
            return Task.FromResult(_dbSet.Update(obj));
        }

        public async Task<List<T>> FindAllAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.Where(predicate).ToListAsync();
        }

        public async Task<T> FindAsync(Expression<Func<T, bool>> predicate)
        {
            var result = await _dbSet.FirstOrDefaultAsync(predicate);
            return result ?? throw new ErrorException(
                StatusCodes.Status404NotFound, 
                ResponseCodeConstants.NOT_FOUND, 
                "Entity not found");
        }

        public async Task<IQueryable<T>> GetAllQueryableAsync()
        {
            return await Task.FromResult(_dbSet.AsQueryable());
        }

        public IQueryable<T> GetQueryable()
        {
            return _dbSet.AsQueryable();
        }
    }
}
