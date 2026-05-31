using System;
using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

[DbTable("permission_collections")]
public sealed class PermissionCollectionEntity
{
    [DbColumn(IsPrimaryKey = true, IsIdentity = true, IsNullable = false)]
    public int Id { get; set; }

    [DbColumn(IsNullable = false, Length = 64)]
    public string Name { get; set; } = string.Empty;

    [DbColumn(IsNullable = false, DataType = "timestamp")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
