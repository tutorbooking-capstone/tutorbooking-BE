using App.Core.Provider;
using App.Repositories.Context;
using Microsoft.EntityFrameworkCore.Storage;
using System.Threading;

namespace App.Repositories.UoW
{
    public class UnitOfWork(AppDbContext dbContext, ICurrentUserProvider currentUserProvider) : IUnitOfWork
    {
        private bool disposed = false;
        private readonly AppDbContext _dbContext = dbContext;
        private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;
        private IDbContextTransaction? _currentTransaction;

        public void BeginTransaction()
        {
            _currentTransaction = _dbContext.Database.BeginTransaction();
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _currentTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        public void CommitTransaction()
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Commit();
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.CommitAsync(cancellationToken);
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    if (_currentTransaction != null)
                    {
                        _currentTransaction.Dispose();
                        _currentTransaction = null;
                    }
                    _dbContext.Dispose();
                }
            }
            disposed = true;
        }

        public void RollBack()
        {
            if (_currentTransaction != null)
            {
                _currentTransaction.Rollback();
                _currentTransaction.Dispose();
                _currentTransaction = null;
            }
        }

        public async Task RollBackAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        public IGenericRepository<T> GetRepository<T>() where T : class
        {
            return new GenericRepository<T>(_dbContext, _currentUserProvider);
        }

        public async Task ExecuteInTransactionAsync(Func<Task> action, Action<Exception>? onError = null, CancellationToken cancellationToken = default)
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                await action();
                await CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await RollBackAsync(cancellationToken);
                onError?.Invoke(ex);
                throw; 
            }
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken cancellationToken = default)
        {
            await BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await action();
                await CommitTransactionAsync(cancellationToken);
                return result;
            }
            catch (Exception ex)
            {
                await RollBackAsync(cancellationToken);
                onError?.Invoke(ex);
                throw;  
            }
        }
    }
}
