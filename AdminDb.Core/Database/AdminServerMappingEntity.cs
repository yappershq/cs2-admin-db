using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

[DbTable("admin_servers_mapping")]
public sealed class AdminServerMappingEntity
{
    [DbColumn(IsPrimaryKey = true, IsIdentity = true, IsNullable = false)]
    public int Id { get; set; }

    [DbColumn(IsNullable = false)]
    public int AdminId { get; set; }

    [DbColumn(IsNullable = true)]
    public int? ServerId { get; set; }

    [DbColumn(IsNullable = true, Length = 64)]
    public string? RoleName { get; set; }
}
