using System.Data.Common;
using Evently.Common.Application.Data;
using Npgsql;

namespace Evently.Common.Infrastructure.Data;

public sealed class DbConnectionFactory(NpgsqlDataSource dataSource) : IDbConnectionFactory
{
    public async ValueTask<DbConnection> OpenConnectionAsync()
    {
        return await dataSource.OpenConnectionAsync();
    }
}
