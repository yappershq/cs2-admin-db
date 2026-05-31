using System;
using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

[DbTable("admin_servers")]
public sealed class AdminServerEntity
{
    [DbColumn(IsPrimaryKey = true, IsIdentity = true, IsNullable = false)]
    public int Id { get; set; }

    [DbColumn(IsNullable = false, Length = 32)]
    public string Tag { get; set; } = string.Empty;

    [DbColumn(IsNullable = false, Length = 128)]
    public string DisplayName { get; set; } = string.Empty;

    [DbColumn(IsNullable = false, DataType = "timestamp")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
