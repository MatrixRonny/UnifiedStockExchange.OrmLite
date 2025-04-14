# This project is forked from [OrmLite](https://github.com/ServiceStack/ServiceStack.OrmLite)

### Introduction

This fork was originally created to update the SQLite implmentation to support custom table naming, so that it is possible to use OrmLite with the same type over multiple database tables.

Here is a code example:
```csharp
OrmLiteConnectionFactory connectionFactory = new(...);
new TableDataAccess<PriceCandle>(_connectionFactory, "EURUSD");
new TableDataAccess<PriceCandle>(_connectionFactory, "GBPUSD");
```

Note that both tables use `PriceCandle` as POCO.

### Why OrmLite?

OrmLite was chosen for this task for the following reasons:
* Can be used with LINQ, although it has limitations
* Simple code-to-sql design without added complexity
* Application code is portable across multiple databases
* Support SQL generation for new runtime types
* OrmLite code is modular, understandable and open

### State of changes

At this point, the following projects are mandatory to compile an pass tests:
* Sqlite
* MySql

#### Project ServiceStack.OrmLite

Project `ServiceStack.OrmLite` is the existing core project and it uses `IOrmLiteDialectProvider` to abstract different SQL implementations. In addition, the core project contains code that puts together configuration, SQL segments and data to form an executable SQL statement.

This project has been adjusted expose `string tableName` present in `IOrmLiteDialectProvider` interface to `OrmLiteWriteCommandExtensions` and `TableDataAccess` implementation. The latter was added in this fork and is recommended for use because the syntax is more consistents across mehtod calls.

The class `TableDataAccess` intends to replicate the dotnet built-in [DbSet](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbset-1) class, but with support for custom table names and multiple sets per table.
