using System.Text.Json;
using EDFCatalogoTablasNet.Configuration;

namespace EDFCatalogoTablasNet.Tests;

public sealed class MongoDbSettingsConfigurationTests
{
    [Fact]
    public void Deserializes_json_snippet_like_MongoDB_section()
    {
        const string json = """
            {
              "ConnectionString": "mongodb://127.0.0.1:27017",
              "DatabaseName": "test_db",
              "UsersCollection": "users",
              "CatalogsCollection": "catalogs",
              "ContactsCollection": "contacts"
            }
            """;

        var s = JsonSerializer.Deserialize<MongoDbSettings>(json);

        Assert.NotNull(s);
        Assert.Equal("mongodb://127.0.0.1:27017", s!.ConnectionString);
        Assert.Equal("test_db", s.DatabaseName);
        Assert.Equal("users", s.UsersCollection);
        Assert.Equal("catalogs", s.CatalogsCollection);
        Assert.Equal("contacts", s.ContactsCollection);
    }
}
