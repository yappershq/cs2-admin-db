using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using AdminDb.Core.Configuration;
using AdminDb.Core.Database;
using AdminDb.Database.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sharp.Modules.AdminManager.Shared;

namespace AdminDb.Core.Modules;

internal sealed class AdminMountManager : IModule
{
    private readonly InterfaceBridge                _bridge;
    private readonly ILogger<AdminMountManager>     _logger;
    private readonly AdminDbConfig                  _config;
    private readonly AdminDbRepository              _repository;

    private IAdminManager? _adminManager;
    private SharedInterfaceModule? _shared;

    private int   _myServerId;
    private Timer? _timer;
    private string _lastHash = string.Empty;

    public AdminMountManager(
        InterfaceBridge             bridge,
        ILogger<AdminMountManager>  logger,
        AdminDbConfig               config,
        AdminDbRepository           repository)
    {
        _bridge     = bridge;
        _logger     = logger;
        _config     = config;
        _repository = repository;
    }

    public bool Init() => true;

    public void OnAllModulesLoaded(ServiceProvider provider)
    {
        _shared = provider.GetRequiredService<SharedInterfaceModule>();

        _adminManager = _bridge.SharpModuleManager
            .GetOptionalSharpModuleInterface<IAdminManager>(IAdminManager.Identity)?.Instance;

        if (_adminManager is null)
        {
            _logger.LogError("[AdminDb] IAdminManager not found — AdminDb cannot mount admins. Aborting.");
            return;
        }

        var dbProvider = _bridge.SharpModuleManager
            .GetOptionalSharpModuleInterface<IDatabaseProvider>(IDatabaseProvider.Identity)?.Instance;

        if (dbProvider is null)
        {
            _logger.LogError("[AdminDb] IDatabaseProvider not found — AdminDb cannot load from DB. Aborting.");
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                _repository.InitSchema();

                var serverId = await _repository.GetOrCreateServerIdAsync(_config.ServerTag);
                if (serverId is null)
                {
                    _logger.LogError("[AdminDb] Failed to resolve ServerId for tag '{Tag}'", _config.ServerTag);
                    return;
                }

                _myServerId = serverId.Value;
                _logger.LogInformation("[AdminDb] ServerTag={Tag} -> ServerId={Id}", _config.ServerTag, _myServerId);

                await BuildAndMountAsync();

                var interval = TimeSpan.FromSeconds(Math.Max(10, _config.RefreshIntervalSeconds));
                _timer = new Timer(_ => _ = Task.Run(BuildAndMountAsync), null, interval, interval);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "[AdminDb] Startup init failed");
            }
        });
    }

    internal async Task BuildAndMountAsync()
    {
        try
        {
            var snapshot = await _repository.LoadSnapshotAsync(_myServerId);
            var manifest = BuildManifest(snapshot);
            var hash     = ComputeHash(manifest);

            if (hash == _lastHash)
            {
                _logger.LogDebug("[AdminDb] No delta — skipping mount");
                return;
            }

            _lastHash = hash;

            var captured = manifest;

            _bridge.ModSharp.InvokeFrameAction(() =>
            {
                _adminManager!.MountAdminManifest("AdminDb", () => captured);

                if (_config.WriteSnapshotJsonc)
                    WriteSnapshot(captured);

                _shared?.RaiseRefreshed();

                _logger.LogInformation("[AdminDb] Mounted {Admins} admins, {Roles} roles, {Cols} permission collections",
                    captured.Admins.Count, captured.Roles.Count, captured.PermissionCollection.Count);
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "[AdminDb] BuildAndMountAsync failed");
        }
    }

    private AdminTableManifest BuildManifest(AdminDbSnapshot snap)
    {
        var permCollections = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var col in snap.Collections)
        {
            if (!snap.RelevantCollectionIds.Contains(col.Id))
                continue;

            var perms = snap.CollectionItems
                .Where(i => i.CollectionId == col.Id)
                .Select(i => i.Permission)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            permCollections[col.Name] = perms;
        }

        var roles = new List<RoleManifest>();

        foreach (var role in snap.Roles)
        {
            if (!snap.RelevantRoleIds.Contains(role.Id))
                continue;

            var perms = snap.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .Select(rp => rp.Permission)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            roles.Add(new RoleManifest(role.Name, role.Immunity, perms));
        }

        var admins = new List<AdminManifest>();

        foreach (var admin in snap.Admins)
        {
            if (!snap.RelevantAdminIds.Contains(admin.Id))
                continue;

            var perms = snap.AdminPermissions
                .Where(ap => ap.AdminId == admin.Id)
                .Select(ap => ap.Permission)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            admins.Add(new AdminManifest(admin.SteamId, admin.Immunity, perms));
        }

        return new AdminTableManifest(permCollections, roles, admins);
    }

    private static string ComputeHash(AdminTableManifest manifest)
    {
        var sb = new StringBuilder();

        foreach (var kv in manifest.PermissionCollection.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sb.Append(kv.Key).Append(':');
            foreach (var p in kv.Value.OrderBy(x => x, StringComparer.Ordinal))
                sb.Append(p).Append(',');
        }

        foreach (var r in manifest.Roles.OrderBy(r => r.Name, StringComparer.Ordinal))
        {
            sb.Append(r.Name).Append(':').Append(r.Immunity).Append(':');
            foreach (var p in r.Permissions.OrderBy(x => x, StringComparer.Ordinal))
                sb.Append(p).Append(',');
        }

        foreach (var a in manifest.Admins.OrderBy(a => a.Identity))
        {
            sb.Append(a.Identity).Append(':').Append(a.Immunity).Append(':');
            foreach (var p in a.Permissions.OrderBy(x => x, StringComparer.Ordinal))
                sb.Append(p).Append(',');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes);
    }

    private void WriteSnapshot(AdminTableManifest manifest)
    {
        try
        {
            var path = Path.Combine(_bridge.SharpPath, "configs", "admins.jsonc.snapshot");

            var doc = new SnapshotDoc
            {
                PermissionCollection = manifest.PermissionCollection
                    .ToDictionary(kv => kv.Key, kv => kv.Value.ToList()),
                Roles = manifest.Roles.Select(r => new SnapshotRole
                {
                    Name        = r.Name,
                    Immunity    = r.Immunity,
                    Permissions = r.Permissions.ToList(),
                }).ToList(),
                Admins = manifest.Admins.Select(a => new SnapshotAdmin
                {
                    Identity    = a.Identity,
                    Immunity    = a.Immunity,
                    Permissions = a.Permissions.ToList(),
                }).ToList(),
            };

            var opts = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(doc, opts));
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "[AdminDb] Failed to write admins.jsonc.snapshot");
        }
    }

    public void Shutdown()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private sealed class SnapshotDoc
    {
        [JsonPropertyName("PermissionCollection")]
        public Dictionary<string, List<string>> PermissionCollection { get; set; } = new();

        [JsonPropertyName("Roles")]
        public List<SnapshotRole> Roles { get; set; } = new();

        [JsonPropertyName("Admins")]
        public List<SnapshotAdmin> Admins { get; set; } = new();
    }

    private sealed class SnapshotRole
    {
        [JsonPropertyName("Name")]       public string      Name        { get; set; } = string.Empty;
        [JsonPropertyName("Immunity")]   public byte        Immunity    { get; set; }
        [JsonPropertyName("Permissions")] public List<string> Permissions { get; set; } = new();
    }

    private sealed class SnapshotAdmin
    {
        [JsonPropertyName("Identity")]   public ulong       Identity    { get; set; }
        [JsonPropertyName("Immunity")]   public byte        Immunity    { get; set; }
        [JsonPropertyName("Permissions")] public List<string> Permissions { get; set; } = new();
    }
}
