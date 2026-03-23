namespace EDFCatalogoTablasNet.Configuration
{
    /// <summary>
    /// Configuración de MongoDB
    /// </summary>
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string UsersCollection { get; set; } = string.Empty;
        public string CatalogsCollection { get; set; } = string.Empty;
        public string ContactsCollection { get; set; } = string.Empty;
    }
}

