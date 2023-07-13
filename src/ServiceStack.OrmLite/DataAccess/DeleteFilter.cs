using ServiceStack.OrmLite;
using System;
using System.Data;
using System.Linq.Expressions;

namespace ServiceStack.OrmLite.DataAccess
{
    public class DeleteFilter<T> : IDeleteFilter<T>
    {
        readonly SqlExpression<T> _sqlExpression;
        readonly IDbConnection _connection;

        public DeleteFilter(IDbConnection connection, string tableName)
        {
            _connection = connection;
            _sqlExpression = _connection.From<T>();
            _sqlExpression.ModelDef.Alias = tableName;
        }

        public DeleteFilter<T> Where(Expression<Func<T, bool>> predicate)
        {
            _sqlExpression.Where(predicate);
            return this;
        }

        public void ExecuteDelete()
        {
            lock (_connection)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }
            }

            string deleteStatement = _sqlExpression.ToDeleteRowStatement();

            IDbCommand sqlCmd = _connection.CreateCommand();
            sqlCmd.CommandText = deleteStatement;
            foreach (var dbParam in _sqlExpression.Params)
            {
                sqlCmd.Parameters.Add(dbParam);
            }

            sqlCmd.ExecNonQuery();
        }
    }
}
