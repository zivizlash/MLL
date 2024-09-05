using LiteDB;

namespace MLL.CUI.Db.Extensions;

public static class BsonValueExtensions
{
    public static int GetId(this BsonValue bsonValue)
    {
        return bsonValue.AsInt32;
    }
}
