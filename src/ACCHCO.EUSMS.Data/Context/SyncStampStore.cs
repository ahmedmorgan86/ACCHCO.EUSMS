using Microsoft.Data.Sqlite;

namespace ACCHCO.EUSMS.Data.Context;

public static class SyncStampStore
{
    public const string TableName = "AppSync_Stamp";

    public static void EnsureTable(string connectionString)
    {
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA busy_timeout = 3000;";
                pragma.ExecuteNonQuery();
            }
            EnsureTable(connection);
        }
        catch
        {
            // Best-effort: startup schema setup must never block the application.
        }
    }

    public static long? ReadStamp(string connectionString)
    {
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA busy_timeout = 2000;";
                pragma.ExecuteNonQuery();
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT ChangeStamp FROM {TableName} WHERE Id = 1;";
            var result = command.ExecuteScalar();
            return result == null || result == DBNull.Value ? null : Convert.ToInt64(result);
        }
        catch
        {
            return null;
        }
    }

    public static void Increment(string connectionString)
    {
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var pragma = connection.CreateCommand();
            pragma.CommandText = "PRAGMA busy_timeout = 5000;";
            pragma.ExecuteNonQuery();

            EnsureTable(connection);

            using var command = connection.CreateCommand();
            command.CommandText = $"UPDATE {TableName} SET ChangeStamp = ChangeStamp + 1 WHERE Id = 1;";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Best-effort: a missed stamp only delays sync until the next save.
        }
    }

    private static void EnsureTable(SqliteConnection connection)
    {
        using var create = connection.CreateCommand();
        create.CommandText = $"CREATE TABLE IF NOT EXISTS {TableName} (Id INTEGER PRIMARY KEY, ChangeStamp INTEGER NOT NULL);";
        create.ExecuteNonQuery();

        using var seed = connection.CreateCommand();
        seed.CommandText = $"INSERT OR IGNORE INTO {TableName} (Id, ChangeStamp) VALUES (1, 0);";
        seed.ExecuteNonQuery();
    }
}
