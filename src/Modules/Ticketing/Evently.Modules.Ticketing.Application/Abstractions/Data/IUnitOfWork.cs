using Evently.Common.Domain;

namespace Evently.Modules.Ticketing.Application.Abstractions.Data;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    //Task<DbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task<Result> ExecuteInTransactionAsync(
        Func<Task<Result>> operation,
        CancellationToken cancellationToken = default);
}
