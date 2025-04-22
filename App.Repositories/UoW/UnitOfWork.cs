using App.Repositories.Context;

namespace App.Repositories.UoW
{
    public class UnitOfWork(AppDbContext dbContext) : IUnitOfWork
    {
        private bool disposed = false;
        private readonly AppDbContext _dbContext = dbContext;
        public void BeginTransaction()
        {
            _dbContext.Database.BeginTransaction();
        }

        public void CommitTransaction()
        {
            _dbContext.Database.CommitTransaction();
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
                    _dbContext.Dispose();
            }
            disposed = true;
        }

        public void RollBack()
        {
            _dbContext.Database.RollbackTransaction();
        }

        public async Task SaveAsync()
        {
            await _dbContext.SaveChangesAsync();
        }

        public IGenericRepository<T> GetRepository<T>() where T : class
        {
            return new GenericRepository<T>(_dbContext);
        }
    }
}
