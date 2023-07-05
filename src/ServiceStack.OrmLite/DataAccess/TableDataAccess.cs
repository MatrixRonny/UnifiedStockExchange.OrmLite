using ServiceStack.OrmLite;
using System;
using System.Data;

namespace ServiceStack.OrmLite.DataAccess
{
    public class TableDataAccess<T> : ITableDataAccess<T>, IDisposable
    {
        private readonly IDbConnection _connection;
        private readonly string _tableName;
        IOrmLiteDialectProvider _dialectProvider;

        public TableDataAccess(OrmLiteConnectionFactory connectionFactory, string tableName)
        {
            _connection = connectionFactory.CreateDbConnection();
            _connection.TableAlias(tableName);
            _tableName = tableName;
            _dialectProvider = connectionFactory.DialectProvider;
        }

        public void CreateTable(bool overwrite = false)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TableDataAccess<T>));

            lock (_connection)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }
            }

            _connection.CreateTable<T>(_tableName, overwrite);
        }

        public void DropTable()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TableDataAccess<T>));

            lock (_connection)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }
            }

            _connection.DropTable<T>(_tableName);
        }

        public bool TableExists()
        {
            lock (_connection)
            {
                if (_connection.State == ConnectionState.Closed)
                {
                    _connection.Open();
                }
            }

            return _connection.TableExists(_tableName);
        }

        public ISelectFilter<T> CreateSelectFilter()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TableDataAccess<T>));

            if (!TableExists())
                throw new DataAccessException("Could not find table with name " + _tableName);

            return new SelectFilter<T>(_connection, _tableName);
        }

        public IDeleteFilter<T> CreateDeleteFilter()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TableDataAccess<T>));

            if (!TableExists())
                throw new DataAccessException("Could not find table with name " + _tableName);

            return new DeleteFilter<T>(_connection, _tableName);
        }

        public IUpdateFilter<T> CreateUpdateFilter()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TableDataAccess<T>));

            if (!TableExists())
                throw new DataAccessException("Could not find table with name " + _tableName);

            return new UpdateFilter<T>(_connection, _tableName);
        }

        public void Insert(T entity)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(TableDataAccess<T>));

            if (!TableExists())
                throw new DataAccessException("Could not find table with name " + _tableName);

            IDbCommand sqlCmd = _connection.CreateCommand();
            _dialectProvider.PrepareParameterizedInsertStatement<T>(sqlCmd, tableName: _tableName);
            _dialectProvider.SetParameterValues<T>(sqlCmd, entity);

            sqlCmd.ExecuteNonQuery();
        }

        bool _isDisposed;
        public void Dispose()
        {
            _isDisposed = true;
            _connection.Close();
        }
    }
}
