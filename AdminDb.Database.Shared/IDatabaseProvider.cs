using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AdminDb.Database.Shared;

public interface IDatabaseProvider
{
    const string Identity = "AdminDb.Database";

    void InitTables(params Type[] entityTypes);

    IDatabaseQueryable<T> Queryable<T>() where T : class, new();

    Task<int> InsertAsync<T>(T entity) where T : class, new();
    Task<int> InsertReturnIdentityAsync<T>(T entity) where T : class, new();

    Task<int> UpdateAsync<T>(T entity) where T : class, new();

    Task<int> UpsertAsync<T>(T entity, Expression<Func<T, object>> matchColumns) where T : class, new();

    Task<int> DeleteAsync<T>(Expression<Func<T, bool>> predicate) where T : class, new();

    Task<int> ExecuteSqlAsync(string sql);
}
