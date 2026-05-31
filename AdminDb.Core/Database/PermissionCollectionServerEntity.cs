using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

[DbTable("permission_collection_servers")]
public sealed class PermissionCollectionServerEntity
{
    [DbColumn(IsPrimaryKey = true, IsIdentity = true, IsNullable = false)]
    public int Id { get; set; }

    [DbColumn(IsNullable = false)]
    public int CollectionId { get; set; }

    [DbColumn(IsNullable = true)]
    public int? ServerId { get; set; }
}
