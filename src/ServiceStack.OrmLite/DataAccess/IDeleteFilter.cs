using System;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.DataAccess
{
    public interface IDeleteFilter<T>
    {
        DeleteFilter<T> Where(Expression<Func<T, bool>> predicate);
        void ExecuteDelete();
    }
}