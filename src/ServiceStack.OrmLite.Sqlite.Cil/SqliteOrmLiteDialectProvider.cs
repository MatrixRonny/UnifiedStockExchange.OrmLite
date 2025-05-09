﻿using ServiceStack.OrmLite.Sqlite.Converters;
using System;
using System.Data;
using System.Data.SQLite;

namespace ServiceStack.OrmLite.Sqlite
{
    public class SqliteOrmLiteDialectProvider : SqliteOrmLiteDialectProviderBase
    {
        public static SqliteOrmLiteDialectProvider Instance = new();

        public SqliteOrmLiteDialectProvider()
        {
            base.RegisterConverter<DateTime>(new SqliteDataDateTimeConverter());
            base.RegisterConverter<Guid>(new SqliteDataGuidConverter());
        }

        protected override IDbConnection CreateConnection(string connectionString)
        {
            // Microsoft.Data.Sqlite no like
            connectionString = connectionString
                .Replace(";Version=3", "")
                .Replace(";New=True", "")
                .Replace(";Compress=True", "");
            return new SQLiteConnection(connectionString);
        }

        public override IDbDataParameter CreateParam()
        {
            return new SQLiteParameter();
        }
    }
}