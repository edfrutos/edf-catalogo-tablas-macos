using System.Text.RegularExpressions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EDFCatalogoTablasNet.Services;

/// <summary>
/// Busca usuarios en BSON con nombres de campo PascalCase o camelCase (datos mezclados / otros clientes).
/// </summary>
internal static class MongoUserBsonHelper
{
    public static IMongoCollection<BsonDocument> UsersCollection(IMongoDatabase database, string collectionName) =>
        database.GetCollection<BsonDocument>(collectionName);

    public static FilterDefinition<BsonDocument> LoginKeyFilter(string loginKeyTrimmed)
    {
        var esc = Regex.Escape(loginKeyTrimmed);
        var rx = new BsonRegularExpression($"^{esc}$", "i");
        return Builders<BsonDocument>.Filter.Or(
            Builders<BsonDocument>.Filter.Regex("Email", rx),
            Builders<BsonDocument>.Filter.Regex("email", rx),
            Builders<BsonDocument>.Filter.Regex("Username", rx),
            Builders<BsonDocument>.Filter.Regex("username", rx));
    }

    public static string? GetStoredPasswordHash(BsonDocument doc)
    {
        if (doc.TryGetValue("Password", out var p) && p.IsString) return p.AsString;
        if (doc.TryGetValue("password", out p) && p.IsString) return p.AsString;
        return null;
    }

    public static bool GetIsActive(BsonDocument doc)
    {
        if (doc.TryGetValue("IsActive", out var v) && v.IsBoolean) return v.AsBoolean;
        if (doc.TryGetValue("isActive", out v) && v.IsBoolean) return v.AsBoolean;
        return true;
    }

    public static string GetCanonicalEmail(BsonDocument doc)
    {
        if (doc.TryGetValue("Email", out var e) && e.IsString) return e.AsString;
        if (doc.TryGetValue("email", out e) && e.IsString) return e.AsString;
        return string.Empty;
    }

    public static string GetRole(BsonDocument doc)
    {
        if (doc.TryGetValue("Role", out var r) && r.IsString) return r.AsString;
        if (doc.TryGetValue("role", out r) && r.IsString) return r.AsString;
        return "User";
    }
}
