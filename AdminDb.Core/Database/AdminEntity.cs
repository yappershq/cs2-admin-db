using System;
using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

[DbTable("admins")]
public sealed class AdminEntity
{
    [DbColumn(IsPrimaryKey = true, IsIdentity = true, IsNullable = false)]
    public int Id { get; set; }

    [DbColumn(IsNullable = false, DataType = "BIGINT UNSIGNED")]
    public ulong SteamId { get; set; }

    [DbColumn(IsNullable = true, Length = 128)]
    public string? Name { get; set; }

    [DbColumn(IsNullable = false)]
    public byte Immunity { get; set; }

    [DbColumn(IsNullable = true, DataType = "datetime")]
    public DateTime? ExpiresAtUtc { get; set; }

    [DbColumn(IsNullable = true, DataType = "text")]
    public string? Note { get; set; }

    [DbColumn(IsNullable = true, DataType = "BIGINT UNSIGNED")]
    public ulong? CreatedBySteamId { get; set; }

    [DbColumn(IsNullable = false, DataType = "timestamp")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
