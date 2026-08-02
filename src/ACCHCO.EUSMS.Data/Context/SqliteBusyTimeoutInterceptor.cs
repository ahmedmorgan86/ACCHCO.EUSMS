using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ACCHCO.EUSMS.Data.Context;

public class SqliteBusyTimeoutInterceptor : DbConnectionInterceptor
{
    private const int BusyTimeoutMs = 5000;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyPragmas(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ApplyPragmas(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void ApplyPragmas(DbConnection connection)
    {
        if (connection is not SqliteConnection sqlite) return;

        try
        {
            using var command = sqlite.CreateCommand();
            command.CommandText = $"PRAGMA busy_timeout = {BusyTimeoutMs};";
            command.ExecuteNonQuery();
        }
        catch
        {
            // PRAGMA setup must never break a connection.
        }
    }
}
