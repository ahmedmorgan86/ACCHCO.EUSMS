using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ACCHCO.EUSMS.Data.Context;

public static class SequenceNumberStore
{
    public const string TableName = "NumberSequence";

    public static void EnsureTable(string connectionString)
    {
        try
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA busy_timeout = 5000;";
                pragma.ExecuteNonQuery();
            }
            using var command = connection.CreateCommand();
            command.CommandText = $"CREATE TABLE IF NOT EXISTS {TableName} (SeqKey TEXT NOT NULL, SeqDate TEXT NOT NULL, LastNumber INTEGER NOT NULL, PRIMARY KEY (SeqKey, SeqDate));";
            command.ExecuteNonQuery();
        }
        catch
        {
            // Best-effort: startup schema setup must never block the application.
        }
    }

    public static async Task<long> NextAsync(EusmsDbContext context, string seqKey, string dateKey,
        string tableName, string numberColumn, string fullPrefix)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();

        const int maxAttempts = 5;
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                await using var command = connection.CreateCommand();
                command.CommandText = $@"
CREATE TABLE IF NOT EXISTS {TableName} (SeqKey TEXT NOT NULL, SeqDate TEXT NOT NULL, LastNumber INTEGER NOT NULL, PRIMARY KEY (SeqKey, SeqDate));
INSERT OR IGNORE INTO {TableName} (SeqKey, SeqDate, LastNumber)
SELECT @seqKey, @dateKey,
       COALESCE((SELECT CAST(substr(""{numberColumn}"", @prefixLen + 1) AS INTEGER) FROM ""{tableName}"" WHERE ""{numberColumn}"" LIKE @prefix ORDER BY ""{numberColumn}"" DESC LIMIT 1), 0)
WHERE NOT EXISTS (SELECT 1 FROM {TableName} WHERE SeqKey = @seqKey AND SeqDate = @dateKey);
UPDATE {TableName} SET LastNumber = LastNumber + 1
WHERE SeqKey = @seqKey AND SeqDate = @dateKey
RETURNING LastNumber;";

                AddParameter(command, "@seqKey", seqKey);
                AddParameter(command, "@dateKey", dateKey);
                AddParameter(command, "@prefix", fullPrefix + "%");
                AddParameter(command, "@prefixLen", fullPrefix.Length);

                var result = await command.ExecuteScalarAsync();
                return result == null || result == DBNull.Value ? 1 : Convert.ToInt64(result);
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode is 5 or 6 && attempt < maxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(200 * (attempt + 1)));
            }
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
