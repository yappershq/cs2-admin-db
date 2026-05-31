using System;
using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

[DbTable("admin_roles")]
public sealed class AdminRoleEntity
{
    [DbColumn(IsPrimaryKey = true, IsIdentity = true, IsNullable = false)]
    public int Id { get; set; }

    [DbColumn(IsNullable = false, Length = 64)]
    public string Name { get; set; } = string.Empty;

    [DbColumn(IsNullable = false)]
    public byte Immunity { get; set; }

    [DbColumn(IsNullable = false, DataType = "timestamp")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
