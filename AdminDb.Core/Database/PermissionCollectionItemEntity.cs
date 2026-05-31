using AdminDb.Database.Shared;

namespace AdminDb.Core.Database;

[DbTable("permission_collection_items")]
public sealed class PermissionCollectionItemEntity
{
    [DbColumn(IsPrimaryKey = true, IsIdentity = true, IsNullable = false)]
    public int Id { get; set; }

    [DbColumn(IsNullable = false)]
    public int CollectionId { get; set; }

    [DbColumn(IsNullable = false, Length = 128)]
    public string Permission { get; set; } = string.Empty;
}
