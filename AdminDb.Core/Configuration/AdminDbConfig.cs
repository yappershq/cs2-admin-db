using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace AdminDb.Core.Configuration;

internal sealed class AdminDbConfig
{
    [JsonPropertyName("ServerTag")]
    public string ServerTag { get; set; } = "default";

    [JsonPropertyName("RefreshIntervalSeconds")]
    public int RefreshIntervalSeconds { get; set; } = 60;

    [JsonPropertyName("WriteSnapshotJsonc")]
    public bool WriteSnapshotJsonc { get; set; } = true;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling         = JsonCommentHandling.Skip,
        AllowTrailingCommas         = true,
        WriteIndented               = true,
    };

    public static AdminDbConfig Load(string sharpPath, ILogger logger)
    {
        var path = Path.Combine(sharpPath, "configs", "cs2-admin-db", "cs2-admin-db.jsonc");
        try
        {
            if (!File.Exists(path))
            {
                var def = new AdminDbConfig();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(def, JsonOpts));
                logger.LogInformation("[AdminDb] Wrote default config to {Path}", path);
                return def;
            }

            var cfg = JsonSerializer.Deserialize<AdminDbConfig>(File.ReadAllText(path), JsonOpts);
            if (cfg is null)
            {
                logger.LogError("[AdminDb] cs2-admin-db.jsonc deserialized to null, using defaults");
                return new AdminDbConfig();
            }

            logger.LogInformation("[AdminDb] Loaded config: ServerTag={Tag}, RefreshInterval={Sec}s",
                cfg.ServerTag, cfg.RefreshIntervalSeconds);
            return cfg;
        }
        catch (Exception e)
        {
            logger.LogError(e, "[AdminDb] Failed to load cs2-admin-db.jsonc, using defaults");
            return new AdminDbConfig();
        }
    }
}
