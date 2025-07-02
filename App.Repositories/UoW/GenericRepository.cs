using Microsoft.EntityFrameworkCore;
using App.Core.Base;
using App.Repositories.Context;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using App.Core.Constants;
using App.Core.Provider;

namespace App.Repositories.UoW
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        #region DI Constructor
        protected readonly DbSet<T> _dbSet;
        private readonly ICurrentUserProvider _currentUserProvider;

        public GenericRepository(
            AppDbContext dbContext,
            ICurrentUserProvider currentUserProvider)
        {
            _dbSet = dbContext.Set<T>();
            _currentUserProvider = currentUserProvider;
        }
        #endregion

        public IQueryable<T> Entities => _dbSet;
        public ICurrentUserProvider CurrentUserProvider => _currentUserProvider;

        #region Tracking Methods
        private void ApplyCreateTracking(T entity) => 
            (entity as ITrackable)?.TrackCreate(_currentUserProvider.GetCurrentUserId() ?? "system-track-create");

        private void ApplyUpdateTracking(T entity) => 
            (entity as ITrackable)?.TrackUpdate(_currentUserProvider.GetCurrentUserId() ?? "system-track-update");

        private void ApplySoftDeleteTracking(T entity) => 
            (entity as ITrackable)?.TrackDelete(_currentUserProvider.GetCurrentUserId() ?? "system-track-soft-delete");
        #endregion

        #region Exist Entities
        public IQueryable<T> ExistEntities()
        {
            if (typeof(BaseEntity).IsAssignableFrom(typeof(T)))
                return _dbSet.Where(x => EF.Property<DateTimeOffset?>(x, "DeletedTime") == null);
                
            return _dbSet;
        }

        public async Task<T> GetExistByIdAsync(object id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
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

        public async Task<IEnumerable<T>> GetAllExistAsync(CancellationToken cancellationToken = default)
            => await ExistEntities().ToListAsync(cancellationToken);

        public async Task<IEnumerable<T>> FindAllExistAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            => await ExistEntities()
                    .Where(predicate)
                    .ToListAsync(cancellationToken);
        #endregion

        public async Task<IList<T>> GetAllAsync(CancellationToken cancellationToken = default)
            => await _dbSet.ToListAsync(cancellationToken);

        public async Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
            => await _dbSet.FindAsync(new object[] { id }, cancellationToken);

        public void Insert(T obj)
        {
            ApplyCreateTracking(obj);
            _dbSet.Add(obj);
        }

        public void InsertRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                ApplyCreateTracking(entity);
            }
            _dbSet.AddRange(entities);
        }

        public void Update(T obj)
        {
            ApplyUpdateTracking(obj);
            _dbSet.Update(obj);
        }

        public void UpdateFields(T entity, params Expression<Func<T, object>>[] properties)
        {
            ApplyUpdateTracking(entity);
            
            if (_dbSet.Entry(entity).State == EntityState.Detached)
                _dbSet.Attach(entity);

            var entry = _dbSet.Entry(entity);
            foreach (var property in properties)
            {
                entry.Property(property).IsModified = true;
            }
        }

        public void Delete(T entity, bool isSoftDelete = true)
        {
            if (isSoftDelete && entity is ITrackable)
            {
                ApplySoftDeleteTracking(entity);
                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }
        }

        public void DeleteRange(IEnumerable<T> entities, bool isSoftDelete = true)
        {
            foreach (var entity in entities)
            {
                if (isSoftDelete && entity is ITrackable)
                {
                    ApplySoftDeleteTracking(entity);
                    _dbSet.Update(entity);
                }
                else
                {
                    _dbSet.Remove(entity);
                }
            }
        }

        public async Task DeleteByIdAsync(object id, bool isSoftDelete = true, CancellationToken cancellationToken = default)
        {
            T entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken)
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    "Entity not found");

            if (isSoftDelete && entity is ITrackable)
            {
                ApplySoftDeleteTracking(entity);
                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }
        }

        public async Task<List<T>> FindAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            => await _dbSet.Where(predicate).ToListAsync(cancellationToken);

        public IQueryable<T> GetQueryable()
            => _dbSet.AsQueryable();

        public async Task<T> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
            => await _dbSet.FirstOrDefaultAsync(predicate, cancellationToken) 
                ?? throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ResponseCodeConstants.NOT_FOUND, 
                    "Entity not found");

        public async Task<BasePaginatedList<T>> GetPagging(IQueryable<T> query, int index, int pageSize, CancellationToken cancellationToken = default)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            pageSize = Math.Clamp(pageSize, 1, 100);  
            index = Math.Max(index, 0);

            int count = await query.AsNoTracking().CountAsync(cancellationToken);
            int maxIndex = count == 0 ? 0 : (int)Math.Ceiling(count / (double)pageSize) - 1;
            index = Math.Min(index, maxIndex);

            var items = await query
                .Skip(index * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return new BasePaginatedList<T>(items, count, index, pageSize);
        }
    }
}
