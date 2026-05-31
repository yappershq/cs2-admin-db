using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AdminDb.Database.Shared;

public interface IDatabaseQueryable<T> where T : class, new()
{
    IDatabaseQueryable<T> Where(Expression<Func<T, bool>> predicate);
    IDatabaseQueryable<T> OrderBy(Expression<Func<T, object>> keySelector);
    IDatabaseQueryable<T> OrderByDescending(Expression<Func<T, object>> keySelector);
    IDatabaseQueryable<T> Take(int count);
    Task<List<T>> ToListAsync();
    Task<T?> FirstOrDefaultAsync();
    Task<int> CountAsync();
}
