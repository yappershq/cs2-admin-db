using System;

namespace AdminDb.Database.Shared;

[AttributeUsage(AttributeTargets.Class)]
public sealed class DbTableAttribute(string tableName) : Attribute
{
    public string TableName { get; } = tableName;
}
