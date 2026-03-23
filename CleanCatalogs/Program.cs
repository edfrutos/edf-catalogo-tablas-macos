using MongoDB.Driver;
using MongoDB.Bson;

Console.WriteLine("🔌 Conectando a MongoDB...");

var connectionString = Environment.GetEnvironmentVariable("MONGO_URI")
    ?? throw new InvalidOperationException(
        "Define la variable de entorno MONGO_URI con tu cadena de conexión MongoDB (no versionar credenciales en código).");
var databaseName = "edf_catalogotablas";
var collectionName = "catalogs";

try
{
    var client = new MongoClient(connectionString);
    var database = client.GetDatabase(databaseName);
    var collection = database.GetCollection<BsonDocument>(collectionName);

    // Contar catálogos antes de eliminar
    var countBefore = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
    Console.WriteLine($"📊 Catálogos encontrados: {countBefore}");

    if (countBefore > 0)
    {
        Console.WriteLine("🗑️  Eliminando todos los catálogos...");
        var result = await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
        Console.WriteLine($"✅ Catálogos eliminados: {result.DeletedCount}");
    }
    else
    {
        Console.WriteLine("ℹ️  No hay catálogos para eliminar");
    }

    // Verificar
    var countAfter = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
    Console.WriteLine($"📊 Catálogos restantes: {countAfter}");

    if (countAfter == 0)
    {
        Console.WriteLine("🎉 ¡Base de datos limpiada exitosamente!");
    }
    else
    {
        Console.WriteLine("⚠️  Advertencia: Aún quedan catálogos en la base de datos");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}

