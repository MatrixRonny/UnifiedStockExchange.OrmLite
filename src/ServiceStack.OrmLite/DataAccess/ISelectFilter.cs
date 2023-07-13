using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.DataAccess
{
    public interface ISelectFilter<T>
    {
        SelectFilter<T> Distinct(Expression<Func<T, object>> selector);
        SelectFilter<T> OrderBy(Expression<Func<T, object>> selector);
        SelectFilter<T> OrderByDescending(Expression<Func<T, object>> selector);
        SelectFilter<T> Skip(int count);
        SelectFilter<T> Take(int count);
        SelectFilter<T> ThenOrderBy(Expression<Func<T, object>> selector);
        SelectFilter<T> Where(Expression<Func<T, bool>> predicate);
        IList<T> ExecuteSelect();
    }
}