using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

[DbTable("admin_role_permissions")]
public sealed class AdminRolePermissionEntity
{
    [DbColumn(IsPrimaryKey = true, IsIdentity = true, IsNullable = false)]
    public int Id { get; set; }

    [DbColumn(IsNullable = false)]
    public int RoleId { get; set; }

    [DbColumn(IsNullable = false, Length = 128)]
    public string Permission { get; set; } = string.Empty;
}
