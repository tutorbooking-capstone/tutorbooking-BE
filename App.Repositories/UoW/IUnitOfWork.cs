using Microsoft.EntityFrameworkCore;

namespace App.Repositories.UoW
{
    public interface IUnitOfWork : IDisposable
    {
        IGenericRepository<T> GetRepository<T>() where T : class;
        Task SaveAsync(CancellationToken cancellationToken = default);
        
        void BeginTransaction();
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        void CommitTransaction();
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        void RollBack();
        Task RollBackAsync(CancellationToken cancellationToken = default);


        Task ExecuteInTransactionAsync(Func<Task> action, Action<Exception>? onError = null, CancellationToken cancellationToken = default);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, Action<Exception>? onError = null, CancellationToken cancellationToken = default);
        Task<T> ExecuteWithConnectionReuseAsync<T>(Func<Task<T>> operation);
    }
}
