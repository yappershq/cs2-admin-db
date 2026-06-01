using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using AdminDb.Database.Shared;
using SqlSugar;

namespace AdminDb.Database.Provider;

internal sealed class SqlSugarAdminDbProvider(ISqlSugarClient db) : IDatabaseProvider
{
    public void InitTables(params Type[] entityTypes)
    {
        db.CodeFirst.SetStringDefaultLength(128).InitTables(entityTypes);
    }

    public IDatabaseQueryable<T> Queryable<T>() where T : class, new()
    {
        return new SqlSugarQueryable<T>(db.Queryable<T>());
    }

    public async Task<int> InsertAsync<T>(T entity) where T : class, new()
    {
        return await db.Insertable(entity).ExecuteCommandAsync();
    }

    public async Task<int> InsertReturnIdentityAsync<T>(T entity) where T : class, new()
    {
        return await db.Insertable(entity).ExecuteReturnIdentityAsync();
    }

    public async Task<int> UpdateAsync<T>(T entity) where T : class, new()
    {
        return await db.Updateable(entity).ExecuteCommandAsync();
    }

    public async Task<int> UpsertAsync<T>(T entity, Expression<Func<T, object>> matchColumns) where T : class, new()
    {
        return await db.Storageable(entity).WhereColumns(matchColumns).ExecuteCommandAsync();
    }

    public async Task<int> DeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        return await db.Deleteable<T>().Where(predicate).ExecuteCommandAsync();
    }

    public async Task<int> ExecuteSqlAsync(string sql)
    {
        return await db.Ado.ExecuteCommandAsync(sql);
    }

    internal static ConnectionConfig BuildConnectionConfig(
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        var dbTypeStr = configuration["Database:Type"] ?? "mysql";
        var host      = configuration["Database:Host"] ?? "localhost";
        var port      = configuration["Database:Port"] ?? "3306";
        var database  = configuration["Database:Database"] ?? "cs2admins";
        var user      = configuration["Database:User"] ?? "root";
        var password  = configuration["Database:Password"] ?? "";

        var dbType = dbTypeStr.ToLowerInvariant() switch
        {
            "mysql"      => DbType.MySql,
            "postgresql" => DbType.PostgreSQL,
            _ => throw new NotSupportedException($"Database type '{dbTypeStr}' is not supported. Supported types: mysql, postgresql"),
        };

        var connectionString = dbType switch
        {
            DbType.MySql      => $"Server={host};Port={port};Database={database};User={user};Password={password};",
            DbType.PostgreSQL => $"Host={host};Port={port};Database={database};Username={user};Password={password};",
            _ => throw new NotSupportedException($"Database type '{dbTypeStr}' is not supported."),
        };

        return new ConnectionConfig
        {
            DbType                = dbType,
            ConnectionString      = connectionString,
            IsAutoCloseConnection = true,
            InitKeyType           = InitKeyType.Attribute,
            MoreSettings          = new ConnMoreSettings { DisableNvarchar = true },
            LanguageType          = LanguageType.English,
            ConfigureExternalServices = new ConfigureExternalServices
            {
                EntityNameService = (type, entity) =>
                {
                    var attr = type.GetCustomAttribute<DbTableAttribute>();
                    if (attr != null)
                        entity.DbTableName = attr.TableName;
                },
                EntityService = (prop, column) =>
                {
                    var attr = prop.GetCustomAttribute<DbColumnAttribute>();
                    if (attr == null) return;

                    if (attr.IsPrimaryKey) column.IsPrimarykey = true;
                    if (attr.IsIdentity)   column.IsIdentity   = true;
                    column.IsNullable = attr.IsNullable;
                    if (attr.IsPrimaryKey || attr.IsIdentity) column.IsNullable = false;
                    if (attr.Length > 0)                       column.Length     = attr.Length;
                    if (!string.IsNullOrEmpty(attr.DataType))  column.DataType   = attr.DataType;
                },
            },
        };
    }
}

internal sealed class SqlSugarQueryable<T>(ISugarQueryable<T> inner) : IDatabaseQueryable<T>
    where T : class, new()
{
    public IDatabaseQueryable<T> Where(Expression<Func<T, bool>> predicate)
    {
        return new SqlSugarQueryable<T>(inner.Where(predicate));
    }

    public IDatabaseQueryable<T> OrderBy(Expression<Func<T, object>> keySelector)
    {
        return new SqlSugarQueryable<T>(inner.OrderBy(keySelector));
    }

    public IDatabaseQueryable<T> OrderByDescending(Expression<Func<T, object>> keySelector)
    {
        return new SqlSugarQueryable<T>(inner.OrderBy(keySelector, OrderByType.Desc));
    }

    public IDatabaseQueryable<T> Take(int count)
    {
        return new SqlSugarQueryable<T>(inner.Take(count));
    }

    public async Task<List<T>> ToListAsync()
    {
        return await inner.ToListAsync();
    }

    public async Task<T?> FirstOrDefaultAsync()
    {
        return await inner.FirstAsync();
    }

    public async Task<int> CountAsync()
    {
        return await inner.CountAsync();
    }
}
