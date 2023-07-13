using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.DataAccess
{
    public class SelectFilter<T> : ISelectFilter<T>
    {
        readonly SqlExpression<T> _sqlExpression;
        readonly IDbConnection _connection;

        public SelectFilter(IDbConnection connection, string tableName)
        {
            _connection = connection;
            _sqlExpression = _connection.From<T>(tableName);
        }

        public SelectFilter<T> Where(Expression<Func<T, bool>> predicate)
        {
            _sqlExpression.Where(predicate);
            return this;
        }

        public SelectFilter<T> OrderBy(Expression<Func<T, object>> selector)
        {
            _sqlExpression.OrderBy(selector);
            return this;
        }

        public SelectFilter<T> ThenOrderBy(Expression<Func<T, object>> selector)
        {
            _sqlExpression.ThenBy(selector);
            return this;
        }

        public SelectFilter<T> Distinct(Expression<Func<T, object>> selector)
        {
            _sqlExpression.SelectDistinct(selector);
            return this;
        }

        public SelectFilter<T> Skip(int count)
        {
            _sqlExpression.Skip(count);
            return this;
        }

        public SelectFilter<T> Take(int count)
        {
            _sqlExpression.Take(count);
            return this;
        }

        public IList<T> ExecuteSelect()
        {
            lock (_connection)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }
            }

            string selectStatement = _sqlExpression.ToSelectStatement();

            IDbCommand sqlCmd = _connection.CreateCommand();
            sqlCmd.CommandText = selectStatement;
            foreach (var dbParam in _sqlExpression.Params)
            {
                sqlCmd.Parameters.Add(dbParam);
            }

            using IDataReader reader = sqlCmd.ExecuteReader();
            return reader.ConvertToList<T>(_sqlExpression.DialectProvider);
        }
    }
}
