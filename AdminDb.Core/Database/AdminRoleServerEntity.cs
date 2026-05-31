using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

[DbTable("admin_role_servers")]
public sealed class AdminRoleServerEntity
{
    [DbColumn(IsPrimaryKey = true, IsIdentity = true, IsNullable = false)]
    public int Id { get; set; }

    [DbColumn(IsNullable = false)]
    public int RoleId { get; set; }

    [DbColumn(IsNullable = true)]
    public int? ServerId { get; set; }
}
