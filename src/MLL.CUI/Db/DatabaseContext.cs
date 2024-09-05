using LiteDB;
using MLL.CUI.Models;

namespace MLL.CUI.Db;

public class DatabaseContext
{
    private readonly ILiteDatabase _database;

    public ILiteCollection<NetInfo> Net => _database.GetCollection<NetInfo>();
    public ILiteCollection<NetLayerStructure> Structure => _database.GetCollection<NetLayerStructure>();
    public ILiteCollection<NetDataSource> Source => _database.GetCollection<NetDataSource>();
    public ILiteCollection<NetUpdateInfo> Update => _database.GetCollection<NetUpdateInfo>();

    public DatabaseContext(ILiteDatabase database)
    {
        _database = database;
    }

    public TOut Execute<TOut>(Func<DatabaseContext, TOut> func)
    {
        return ExecuteInTransaction(func, this);
    }

    public TOut Execute<TOut>(Func<DatabaseContext, Func<TOut>> func)
    {
        return ExecuteInTransaction(
            (object? _) => func.Invoke(this).Invoke(), null);
    }

    private TOut ExecuteInTransaction<TIn, TOut>(Func<TIn, TOut> func, TIn arg)
    {
        if (!_database.BeginTrans())
            throw new InvalidOperationException("Failed to open transaction");

        var result = func.Invoke(arg);

        if (!_database.Commit())
            throw new InvalidOperationException("Failed to commit transaction");

        return result;
    }
}
