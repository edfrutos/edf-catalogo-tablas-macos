using EDFCatalogoTablasNet.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;

namespace EDFCatalogoTablasNet.Services
{
    /// <summary>
    /// Servicio para inicializar datos en la base de datos
    /// </summary>
    public class DatabaseSeeder
    {
        private readonly MongoDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(MongoDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                await TryEnvPasswordResetAsync();

                // Verificar si ya existe el usuario admin
                var adminExists = await _context.Users
                    .Find(u => u.Email == "admin@edf.com")
                    .AnyAsync();

                if (!adminExists)
                {
                    // Crear usuario administrador
                    var adminUser = new User
                    {
                        Name = "Administrador",
                        Email = "admin@edf.com",
                        Password = HashPassword("admin123"),
                        Role = "Admin",
                        Company = "EDF Proyectos",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    await _context.Users.InsertOneAsync(adminUser);
                    _logger.LogInformation("✅ Usuario administrador creado: admin@edf.com / admin123");
                }

                // Verificar si existen catálogos de ejemplo
                var catalogsCount = await _context.Catalogs.CountDocumentsAsync(_ => true);

                if (catalogsCount == 0)
                {
                    // Crear catálogos de ejemplo
                    var sampleCatalogs = new[]
                    {
                        new Catalog
                        {
                            Name = "Productos Electrónicos",
                            Description = "Catálogo de productos electrónicos disponibles en nuestra tienda",
                            Category = "Inventario",
                            Fecha = DateTime.Today.AddDays(-2),
                            DocumentoUrl = "https://ejemplo.com/catalogo-productos.pdf",
                            ImagenUrl = "https://via.placeholder.com/300x200/0066cc/ffffff?text=Productos+Electronicos",
                            Headers = new[] { "Nombre", "Precio", "Categoría", "Stock" },
                            Rows = new List<CatalogRow>
                            {
                                new() { Data = new() { ["Nombre"] = "Laptop Dell XPS", ["Precio"] = "899.99", ["Categoría"] = "Computadoras", ["Stock"] = "15" }, CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow.AddDays(-2) },
                                new() { Data = new() { ["Nombre"] = "Mouse Logitech MX", ["Precio"] = "25.50", ["Categoría"] = "Periféricos", ["Stock"] = "50" }, CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow.AddDays(-2) }
                            },
                            CreatedBy = "admin",
                            Owner = "admin",
                            CreatedAt = DateTime.UtcNow.AddDays(-2),
                            UpdatedAt = DateTime.UtcNow.AddDays(-1)
                        },
                        new Catalog
                        {
                            Name = "Empleados Departamento IT",
                            Description = "Lista completa de empleados del departamento de tecnología con sus datos de contacto",
                            Category = "Recursos Humanos",
                            Fecha = DateTime.Today.AddDays(-5),
                            DocumentoUrl = "https://ejemplo.com/organigrama-it.pdf",
                            MultimediaUrl = "https://ejemplo.com/video-presentacion-equipo.mp4",
                            ImagenUrl = "https://via.placeholder.com/300x200/28a745/ffffff?text=Equipo+IT",
                            Headers = new[] { "Nombre", "Posición", "Email", "Teléfono" },
                            Rows = new List<CatalogRow>
                            {
                                new() { Data = new() { ["Nombre"] = "Ana García", ["Posición"] = "Desarrolladora Senior", ["Email"] = "ana.garcia@empresa.com", ["Teléfono"] = "+34 600 123 456" }, CreatedAt = DateTime.UtcNow.AddDays(-5), UpdatedAt = DateTime.UtcNow.AddDays(-5) },
                                new() { Data = new() { ["Nombre"] = "Carlos López", ["Posición"] = "DevOps Engineer", ["Email"] = "carlos.lopez@empresa.com", ["Teléfono"] = "+34 600 789 012" }, CreatedAt = DateTime.UtcNow.AddDays(-5), UpdatedAt = DateTime.UtcNow.AddDays(-5) }
                            },
                            CreatedBy = "admin",
                            Owner = "admin",
                            CreatedAt = DateTime.UtcNow.AddDays(-5),
                            UpdatedAt = DateTime.UtcNow.AddDays(-3)
                        }
                    };

                    await _context.Catalogs.InsertManyAsync(sampleCatalogs);
                    _logger.LogInformation("✅ {Count} catálogos de ejemplo creados", sampleCatalogs.Length);
                }

                _logger.LogInformation("🎉 Base de datos inicializada correctamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error al inicializar la base de datos");
                throw;
            }
        }

        /// <summary>
        /// Si existen EDF_DEV_PASSWORD_RESET_EMAIL y EDF_DEV_PASSWORD_RESET_PLAIN, hace $set del hash (Pascal/camel en BSON).
        /// Funciona en cualquier ASPNETCORE_ENVIRONMENT; quita las variables tras usar. No commitear la contraseña.
        /// </summary>
        private async Task TryEnvPasswordResetAsync()
        {
            var emailOrUser = Environment.GetEnvironmentVariable("EDF_DEV_PASSWORD_RESET_EMAIL")?.Trim();
            var plain = Environment.GetEnvironmentVariable("EDF_DEV_PASSWORD_RESET_PLAIN");
            if (string.IsNullOrEmpty(emailOrUser) || string.IsNullOrEmpty(plain))
                return;

            var collName = _context.Users.CollectionNamespace.CollectionName;
            var bsonUsers = MongoUserBsonHelper.UsersCollection(_context.Database, collName);
            var doc = await bsonUsers
                .Find(MongoUserBsonHelper.LoginKeyFilter(emailOrUser))
                .FirstOrDefaultAsync();

            if (doc == null)
            {
                _logger.LogWarning("EDF_DEV_PASSWORD_RESET_*: no hay documento en '{Collection}' con email/username {Key}", collName, emailOrUser);
                return;
            }

            var hash = HashPassword(plain);
            await bsonUsers.UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", doc["_id"]),
                Builders<BsonDocument>.Update.Set("Password", hash));

            var who = MongoUserBsonHelper.GetCanonicalEmail(doc);
            if (string.IsNullOrEmpty(who)) who = emailOrUser;
            _logger.LogWarning("Contraseña actualizada (hash) para {Who}. Quita EDF_DEV_PASSWORD_RESET_* y reinicia.", who);
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "EDF_SALT_2024"));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}

