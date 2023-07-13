using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.DataAccess
{
    public class UpdateFilter<T> : IUpdateFilter<T>
    {
        readonly SqlExpression<T> _sqlExpression;
        private IDbConnection _connection;

        public UpdateFilter(IDbConnection connection, string tableName)
        {
            _connection = connection;
            _sqlExpression = _connection.From<T>();
            _sqlExpression.ModelDef.Alias = tableName;
        }

        public UpdateFilter<T> Where(Expression<Func<T, bool>> predicate)
        {
            _sqlExpression.Where(predicate);
            return this;
        }

        public void ExecuteUpdate(T entity)
        {
            lock (_connection)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }
            }

            IDbCommand sqlCmd = _connection.CreateCommand();
            _sqlExpression.PrepareUpdateStatement(sqlCmd, entity);
            sqlCmd.ExecNonQuery();
        }

        public void ExecuteUpdate(Dictionary<string, object> updateFields)
        {
            lock (_connection)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }
            }

            IDbCommand sqlCmd = _connection.CreateCommand();
            _sqlExpression.PrepareUpdateStatement(sqlCmd, updateFields);
            sqlCmd.ExecNonQuery();
        }
    }
}
