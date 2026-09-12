using System.Data.Common;

namespace Evently.Modules.Ticketing.Application.Abstractions.Data;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
