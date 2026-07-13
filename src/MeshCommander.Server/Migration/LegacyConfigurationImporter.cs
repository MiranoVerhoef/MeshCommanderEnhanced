using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace MeshCommander.Server.Migration;

public sealed class LegacyConfigurationImporter
{
    private static readonly HashSet<string> ImportedKeys = new(StringComparer.Ordinal)
    {
        "TlsSecurityMode",
        "SkipTlsHostnameCheck",
        "amtcertpins",
        "checkForUpdate",
        "computers",
        "desktopsettings",
        "iderurl",
        "meshserverhash",
        "meshserverurl",
        "meshserveruser",
        "scanForComputers",
        "scanrange"
    };

    private readonly Lazy<LegacyMigrationResult> result;

    public LegacyConfigurationImporter()
    {
        result = new Lazy<LegacyMigrationResult>(ImportCore, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public LegacyMigrationResult Import() => result.Value;

    public static IReadOnlyDictionary<string, string> ReadLegacyDatabase(string databasePath)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Cache = SqliteCacheMode.Private
        };

        using var connection = new SqliteConnection(builder.ConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT key, value FROM ItemTable";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var key = reader.GetString(0);
            if (!ImportedKeys.Contains(key))
            {
                continue;
            }

            var value = DecodeValue(reader.GetValue(1));
            if (key == "computers")
            {
                value = DecodeLegacyEncryptedJson(value) ?? value;
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                values[key] = value;
            }
        }

        return values;
    }

    public static string? DecodeLegacyEncryptedJson(string value)
    {
        if (IsJson(value))
        {
            return value;
        }

        if (value.Length <= 512 || !IsHex(value.AsSpan(512)))
        {
            return null;
        }

        try
        {
            var password = Encoding.UTF8.GetBytes(value[..512]);
            var encrypted = Convert.FromHexString(value[512..]);
            var keyAndIv = DeriveOpenSslKeyAndIv(password, 48);
            var decrypted = TransformAesCtr(encrypted, keyAndIv[..32], keyAndIv[32..48]);
            var json = Encoding.UTF8.GetString(decrypted);
            return IsJson(json) ? json : null;
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    private static LegacyMigrationResult ImportCore()
    {
        foreach (var database in FindLegacyDatabases())
        {
            try
            {
                var values = ReadLegacyDatabase(database);
                if (values.Count > 0)
                {
                    return new LegacyMigrationResult(true, database, values);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SqliteException)
            {
                // Try the next known legacy profile location.
            }
        }

        return new LegacyMigrationResult(false, null, new Dictionary<string, string>());
    }

    private static IEnumerable<string> FindLegacyDatabases()
    {
        var roots = new List<string>();
        if (OperatingSystem.IsWindows())
        {
            roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeshCommander"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "MeshCommander"));
        }
        else
        {
            roots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config", "MeshCommander"));
        }

        foreach (var root in roots.Where(Directory.Exists))
        {
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(root, "*.localstorage", SearchOption.AllDirectories).ToArray();
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            foreach (var file in files.OrderByDescending(File.GetLastWriteTimeUtc))
            {
                yield return file;
            }
        }
    }

    private static string DecodeValue(object value)
    {
        if (value is string text)
        {
            return text;
        }

        if (value is not byte[] bytes || bytes.Length == 0)
        {
            return string.Empty;
        }

        var unicode = Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        return unicode.Any(character => character != '\uFFFD') ? unicode : Encoding.UTF8.GetString(bytes).TrimEnd('\0');
    }

    private static bool IsJson(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind is JsonValueKind.Array or JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool IsHex(ReadOnlySpan<char> value) =>
        value.Length % 2 == 0 && value.Length > 0 && value.IndexOfAnyExcept("0123456789abcdefABCDEF") == -1;

    private static byte[] DeriveOpenSslKeyAndIv(byte[] password, int length)
    {
        var output = new List<byte>(length);
        byte[] previous = [];
        while (output.Count < length)
        {
            var input = new byte[previous.Length + password.Length];
            previous.CopyTo(input, 0);
            password.CopyTo(input, previous.Length);
            previous = MD5.HashData(input);
            output.AddRange(previous);
        }

        return output.Take(length).ToArray();
    }

    private static byte[] TransformAesCtr(byte[] input, byte[] key, byte[] initialCounter)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key;
        using var encryptor = aes.CreateEncryptor();

        var output = new byte[input.Length];
        var counter = (byte[])initialCounter.Clone();
        var keyStream = new byte[16];
        for (var offset = 0; offset < input.Length; offset += 16)
        {
            encryptor.TransformBlock(counter, 0, counter.Length, keyStream, 0);
            var count = Math.Min(16, input.Length - offset);
            for (var index = 0; index < count; index++)
            {
                output[offset + index] = (byte)(input[offset + index] ^ keyStream[index]);
            }

            IncrementCounter(counter);
        }

        return output;
    }

    private static void IncrementCounter(byte[] counter)
    {
        for (var index = counter.Length - 1; index >= 0; index--)
        {
            if (++counter[index] != 0)
            {
                return;
            }
        }
    }
}

public sealed record LegacyMigrationResult(
    bool Migrated,
    string? Source,
    IReadOnlyDictionary<string, string> Values);
