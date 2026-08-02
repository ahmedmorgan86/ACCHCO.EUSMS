using ACCHCO.EUSMS.Data.Context;

namespace ACCHCO.EUSMS.App.Helpers;

public class DbChangeTracker
{
    private readonly string _connectionString;
    private long _lastStamp;

    public DbChangeTracker(string dbPath)
    {
        _connectionString = $"Data Source={dbPath};Pooling=False";
        _lastStamp = ReadStamp() ?? 0;
        Serilog.Log.Information("Sync tracker initialized, stamp: {Stamp}", _lastStamp);
    }

    public bool HasChanged()
    {
        var stamp = ReadStamp();
        if (!stamp.HasValue) return false;
        if (stamp.Value == _lastStamp) return false;

        var previous = _lastStamp;
        _lastStamp = stamp.Value;
        Serilog.Log.Information("Sync: database change detected (stamp {From} -> {To})", previous, _lastStamp);
        return true;
    }

    private long? ReadStamp()
    {
        return SyncStampStore.ReadStamp(_connectionString);
    }
}
