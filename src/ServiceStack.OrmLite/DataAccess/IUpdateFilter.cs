using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.DataAccess
{
    public interface IUpdateFilter<T>
    {
        UpdateFilter<T> Where(Expression<Func<T, bool>> predicate);
        void ExecuteUpdate(Dictionary<string, object> updateFields);
        void ExecuteUpdate(T entity);
    }
}