using MongoDB.Bson;
using MongoDB.Driver;

static string? FirstEnv(params string[] names)
{
    foreach (var name in names)
    {
        var v = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(v))
            return v.Trim();
    }
    return null;
}

static void PrintHelp()
{
    Console.WriteLine("""
        CleanCatalogs — elimina todos los documentos de la colección de catálogos en MongoDB.

        Variables de entorno (cadena de conexión, una obligatoria):
          MONGO_URI o MONGODB_URI     URI de MongoDB (no commitear credenciales).

        Opcionales (mismos nombres que en Swift / documentación del monorepo):
          MONGO_DB o MONGODB_DB                    Nombre de la base (por defecto: edf_catalogotablas).
          CLEAN_CATALOGS_COLLECTION o MONGO_CATALOGS_COLLECTION   Colección (por defecto: catalogs).

        Argumentos:
          --dry-run    Solo muestra recuento; no borra nada.
          --help       Muestra esta ayuda.

        Ejemplo:
          export MONGO_URI='mongodb+srv://...'
          dotnet run --project CleanCatalogs/CleanCatalogs.csproj
          dotnet run --project CleanCatalogs/CleanCatalogs.csproj -- --dry-run
        """);
}

var argsList = args.ToList();
if (argsList.Contains("--help") || argsList.Contains("-h"))
{
    PrintHelp();
    return;
}

var dryRun = argsList.Contains("--dry-run");

var connectionString = FirstEnv("MONGO_URI", "MONGODB_URI")
    ?? throw new InvalidOperationException(
        "Define MONGO_URI o MONGODB_URI con la cadena de conexión MongoDB (no versionar credenciales en código).");

var databaseName = FirstEnv("MONGO_DB", "MONGODB_DB") ?? "edf_catalogotablas";
var collectionName = FirstEnv("CLEAN_CATALOGS_COLLECTION", "MONGO_CATALOGS_COLLECTION") ?? "catalogs";

Console.WriteLine("🔌 Conectando a MongoDB...");
Console.WriteLine($"   Base de datos: {databaseName}");
Console.WriteLine($"   Colección:     {collectionName}");
if (dryRun)
    Console.WriteLine("   Modo:          dry-run (no se eliminará nada)");

try
{
    var client = new MongoClient(connectionString);
    await client.GetDatabase(databaseName).RunCommandAsync<BsonDocument>(
        new BsonDocument("ping", 1),
        cancellationToken: CancellationToken.None);

    var database = client.GetDatabase(databaseName);
    var collection = database.GetCollection<BsonDocument>(collectionName);

    var countBefore = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
    Console.WriteLine($"📊 Documentos en '{collectionName}': {countBefore}");

    if (countBefore == 0)
    {
        Console.WriteLine("ℹ️  No hay documentos que eliminar.");
        return;
    }

    if (dryRun)
    {
        Console.WriteLine("✅ Dry-run finalizado (sin cambios).");
        return;
    }

    Console.WriteLine("🗑️  Eliminando todos los documentos de la colección...");
    var result = await collection.DeleteManyAsync(Builders<BsonDocument>.Filter.Empty);
    Console.WriteLine($"✅ Documentos eliminados: {result.DeletedCount}");

    var countAfter = await collection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
    Console.WriteLine($"📊 Documentos restantes: {countAfter}");

    if (countAfter == 0)
        Console.WriteLine("🎉 Colección vaciada.");
    else
        Console.WriteLine("⚠️  Advertencia: aún quedan documentos (revisa filtros o permisos).");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
