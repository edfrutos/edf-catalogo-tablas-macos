using MongoDB.Bson;
using MongoDB.Driver;
using EDFCatalogoTablasNet.Models;
using EDFCatalogoTablasNet.Configuration;

namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Contexto de MongoDB para gestionar las colecciones
    /// </summary>
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly MongoDbSettings _settings;

        public MongoDbContext(MongoDbSettings settings)
        {
            _settings = settings;
            var client = new MongoClient(_settings.ConnectionString);
            _database = client.GetDatabase(_settings.DatabaseName);
        }

        public IMongoDatabase Database => _database;

        public IMongoCollection<User> Users =>
            _database.GetCollection<User>(_settings.UsersCollection);

        public IMongoCollection<Catalog> Catalogs =>
            _database.GetCollection<Catalog>(_settings.CatalogsCollection);

        /// <summary>Lectura BSON cruda (catálogos legacy con esquema heterogéneo).</summary>
        public IMongoCollection<BsonDocument> CatalogsBson =>
            _database.GetCollection<BsonDocument>(_settings.CatalogsCollection);

        public IMongoCollection<ContactModel> Contacts =>
            _database.GetCollection<ContactModel>(_settings.ContactsCollection);
    }
}

