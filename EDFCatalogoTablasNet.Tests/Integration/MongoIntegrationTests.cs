using MongoDB.Bson;
using MongoDB.Driver;

namespace EDFCatalogoTablasNet.Tests.Integration;

/// <summary>
/// Requiere <c>MongoDB__ConnectionString</c> y opcionalmente <c>MongoDB__DatabaseName</c>.
/// Ejecutar con: <c>dotnet test --filter "FullyQualifiedName~Integration"</c> (p. ej. en CI con Mongo).
/// </summary>
public sealed class MongoIntegrationTests
{
    [Fact]
    public async Task RoundTrip_insert_find_delete()
    {
        var conn = Environment.GetEnvironmentVariable("MongoDB__ConnectionString");
        Assert.False(string.IsNullOrWhiteSpace(conn),
            "MongoDB__ConnectionString debe estar definida para pruebas de integración.");

        var dbName = Environment.GetEnvironmentVariable("MongoDB__DatabaseName") ?? "edf_tests";
        var client = new MongoClient(conn);
        var coll = client.GetDatabase(dbName).GetCollection<BsonDocument>("ci_roundtrip");

        var id = Guid.NewGuid().ToString("N");
        await coll.InsertOneAsync(new BsonDocument { { "_id", id }, { "v", 1 } });

        try
        {
            var found = await coll.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).FirstOrDefaultAsync();
            Assert.NotNull(found);
            Assert.Equal(1, found!["v"].AsInt32);
        }
        finally
        {
            await coll.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id));
        }
    }
}
