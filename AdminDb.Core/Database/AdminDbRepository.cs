using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

internal sealed class AdminDbRepository(IDatabaseProvider db)
{
    internal void InitSchema()
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

        ApplyMigrationsAsync().GetAwaiter().GetResult();
    }

    private async Task ApplyMigrationsAsync()
    {
        foreach (var sql in Migrations)
        {
            try { await db.ExecuteSqlAsync(sql); }
            catch { }
        }
    }

    private static readonly string[] Migrations =
    [
        "ALTER TABLE admins      MODIFY Immunity TINYINT UNSIGNED NOT NULL",
        "ALTER TABLE admin_roles MODIFY Immunity TINYINT UNSIGNED NOT NULL",
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
            adminPermissions);
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
    List<AdminPermissionEntity>            AdminPermissions);
