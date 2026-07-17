using MeshCommander.Server.Migration;
using Microsoft.Data.Sqlite;
using Xunit;

namespace MeshCommander.Server.Tests;

public sealed class LegacyConfigurationImporterTests : IDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"meshcommander-{Guid.NewGuid():N}.localstorage");

    [Fact]
    public void ReadsSupportedChromiumLocalStorageValues()
    {
        using (var connection = new SqliteConnection($"Data Source={databasePath}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE ItemTable (key TEXT UNIQUE ON CONFLICT REPLACE, value BLOB NOT NULL ON CONFLICT FAIL);
                INSERT INTO ItemTable(key, value) VALUES ('computers', $computers);
                INSERT INTO ItemTable(key, value) VALUES ('scanrange', $scanrange);
                INSERT INTO ItemTable(key, value) VALUES ('unrelated', $unrelated);
                """;
            command.Parameters.AddWithValue("$computers", System.Text.Encoding.Unicode.GetBytes("[{\"host\":\"192.168.1.10\"}]"));
            command.Parameters.AddWithValue("$scanrange", System.Text.Encoding.Unicode.GetBytes("192.168.1.0/24"));
            command.Parameters.AddWithValue("$unrelated", System.Text.Encoding.Unicode.GetBytes("ignored"));
            command.ExecuteNonQuery();
        }

        var result = LegacyConfigurationImporter.ReadLegacyDatabase(databasePath);

        Assert.Equal("[{\"host\":\"192.168.1.10\"}]", result["computers"]);
        Assert.Equal("192.168.1.0/24", result["scanrange"]);
        Assert.DoesNotContain("unrelated", result.Keys);
    }

    [Fact]
    public void KeepsPlainJsonComputerLists()
    {
        const string json = "[{\"name\":\"lab\"}]";
        Assert.Equal(json, LegacyConfigurationImporter.DecodeLegacyEncryptedJson(json));
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }
}
