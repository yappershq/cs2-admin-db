using System;

namespace AdminDb.Database.Shared;

[AttributeUsage(AttributeTargets.Property)]
public sealed class DbColumnAttribute : Attribute
{
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity   { get; set; }
    public bool IsNullable   { get; set; } = true;
    public int  Length       { get; set; }
    public string? DataType  { get; set; }
}
