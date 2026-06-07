using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

internal sealed class AdminDbRepository(IDatabaseProvider db)
{
    internal void InitSchema(Action<string, Exception>? onMigrationError = null)
    {
        db.InitTables(
            typeof(AdminServerEntity),
            typeof(PermissionCollectionEntity),
            typeof(PermissionCollectionItemEntity),
            typeof(PermissionCollectionServerEntity),
            typeof(AdminRoleEntity),
            typeof(AdminRolePermissionEntity),
            typeof(AdminRoleServerEntity),
            typeof(AdminEntity),
            typeof(AdminPermissionEntity),
            typeof(AdminServerMappingEntity));

        ApplyMigrationsAsync(onMigrationError).GetAwaiter().GetResult();
    }

    internal async Task ApplyMigrationsAsync(Action<string, Exception>? onError = null)
    {
        foreach (var sql in Migrations)
        {
            try { await db.ExecuteSqlAsync(sql); }
            catch (Exception e)
            {
                // A migration that's already been applied (column/key already exists, or a
                // MODIFY/DROP targeting something absent) is benign — the schema is already in
                // the desired shape (fresh installs get it from CodeFirst.InitTables). Skip
                // silently; only surface genuine failures so they don't masquerade as errors.
                if (IsAlreadyApplied(e))
                    continue;

                onError?.Invoke(sql, e);
            }
        }
    }

    // True when the migration error means "already in the target state" rather than a real failure.
    private static bool IsAlreadyApplied(Exception e)
    {
        var m = e.Message;
        return m.Contains("Duplicate column name", StringComparison.OrdinalIgnoreCase)
            || m.Contains("Duplicate key name",    StringComparison.OrdinalIgnoreCase)
            || m.Contains("check that column/key exists", StringComparison.OrdinalIgnoreCase);
    }

    // Idempotent schema migrations applied at startup. ALTER MODIFY is a no-op
    // when the column already matches. ADD COLUMN throws on existing column —
    // caught at the call site and logged as info, not error.
    //
    // Fresh installs get the right shape via the entity attributes
    // (SqlSugar CodeFirst.InitTables). These run as a belt-and-suspenders for
    // pre-existing databases that pre-date a schema change.
    private static readonly string[] Migrations =
    [
        "ALTER TABLE admins      MODIFY Immunity TINYINT UNSIGNED NOT NULL",
        "ALTER TABLE admin_roles MODIFY Immunity TINYINT UNSIGNED NOT NULL",
        "ALTER TABLE admin_servers_mapping ADD COLUMN RoleName VARCHAR(64) NULL",
    ];

    internal async Task<int?> GetOrCreateServerIdAsync(string tag)
    {
        var existing = await db.Queryable<AdminServerEntity>()
            .Where(s => s.Tag == tag)
            .FirstOrDefaultAsync();

        if (existing is not null)
            return existing.Id;

        var entity = new AdminServerEntity
        {
            Tag         = tag,
            DisplayName = tag,
            CreatedAt   = DateTime.UtcNow,
        };

        var id = await db.InsertReturnIdentityAsync(entity);
        return id;
    }

    internal async Task<AdminDbSnapshot> LoadSnapshotAsync(int serverId)
    {
        var collectionServers = await db.Queryable<PermissionCollectionServerEntity>()
            .Where(cs => cs.ServerId == null || cs.ServerId == serverId)
            .ToListAsync();

        var relevantCollectionIds = collectionServers.Select(cs => cs.CollectionId).Distinct().ToHashSet();

        var collections = await db.Queryable<PermissionCollectionEntity>()
            .ToListAsync();

        var collectionItems = await db.Queryable<PermissionCollectionItemEntity>()
            .ToListAsync();

        var roleServers = await db.Queryable<AdminRoleServerEntity>()
            .Where(rs => rs.ServerId == null || rs.ServerId == serverId)
            .ToListAsync();

        var relevantRoleIds = roleServers.Select(rs => rs.RoleId).Distinct().ToHashSet();

        var roles = await db.Queryable<AdminRoleEntity>()
            .ToListAsync();

        var rolePermissions = await db.Queryable<AdminRolePermissionEntity>()
            .ToListAsync();

        var adminServers = await db.Queryable<AdminServerMappingEntity>()
            .Where(am => am.ServerId == null || am.ServerId == serverId)
            .ToListAsync();

        var relevantAdminIds = adminServers.Select(am => am.AdminId).Distinct().ToHashSet();

        var now = DateTime.UtcNow;
        var admins = await db.Queryable<AdminEntity>()
            .Where(a => a.ExpiresAtUtc == null || a.ExpiresAtUtc > now)
            .ToListAsync();

        var adminPermissions = await db.Queryable<AdminPermissionEntity>()
            .ToListAsync();

        return new AdminDbSnapshot(
            relevantCollectionIds,
            collections,
            collectionItems,
            relevantRoleIds,
            roles,
            rolePermissions,
            relevantAdminIds,
            admins,
            adminPermissions,
            adminServers);
    }
}

internal sealed record AdminDbSnapshot(
    HashSet<int>                           RelevantCollectionIds,
    List<PermissionCollectionEntity>       Collections,
    List<PermissionCollectionItemEntity>   CollectionItems,
    HashSet<int>                           RelevantRoleIds,
    List<AdminRoleEntity>                  Roles,
    List<AdminRolePermissionEntity>        RolePermissions,
    HashSet<int>                           RelevantAdminIds,
    List<AdminEntity>                      Admins,
    List<AdminPermissionEntity>            AdminPermissions,
    List<AdminServerMappingEntity>         AdminServers);
