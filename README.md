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

### How to use the code

At this stage, the code should be consumed as a git submodule. Building the NuGet package requires subtantial work and redesign of the build and configuration files.

### State of changes

At this point, the following projects are mandatory to compile an pass tests (see below):
* Sqlite
* MySql

#### Project ServiceStack.OrmLite

Project `ServiceStack.OrmLite` is the existing core project and it uses `IOrmLiteDialectProvider` to abstract different SQL implementations. In addition, the core project contains code that puts together configuration, SQL segments and data to form an executable SQL statement.

This project has been adjusted expose `string tableName` present in `IOrmLiteDialectProvider` interface to `OrmLiteWriteCommandExtensions` and `TableDataAccess` implementation. The latter was added in this fork and is recommended for use because the syntax is more consistents across mehtod calls.

The class `TableDataAccess` intends to replicate the dotnet [DbSet](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbset-1) class, but with support for custom table names and multiple tables per type.

### Development heads-up

Common code refactorings
* `new OrmLiteConn(DbFactory);` => `DbFactory.CreateDbConnection()`
* `dbFactory.Open()` => `dbFactory.OpenDbConnection()`
* `new SqliteConnection(connectionString)` => `new SQLiteConnection(connectionString)`
* Missing final argument `string tableName = null` in overriden method
* `ToCreateTableStatement(typeof(ModelWithIdAndName))` => `ModelDefinition.CreateInstance<ModelWithIdAndName>()`
* Project file: `<TargetFramework>net481</TargetFramework>` => `<TargetFrameworks>net481;net8.0</TargetFrameworks>`
* Project file: `<TargetFramework>net8.0</TargetFramework>` => `<TargetFrameworks>net8.0</TargetFrameworks>`

Dealing with failing tests
* For now, make sure at least 70% of the tests pass when compiling for dotnet.
* Running the tests affected by the code changes requires installing the corresponding DB engines.

### When everything fails

Clean solution, close VS2022 / VsCode, delete bin and obj folders. Also, consider deleting `.vs` hidden folder found at the root. As a last resort, commit everything into a temporary branch, delete and clone repo again. I'm recommending this because the degree of legacy code and lack of dependency strategy makes compilation become unstable from time to time.

In Windows, bin and obj folders can be found with `filename:=bin OR filename:=obj` pattern. In Linux, I'm sure everyone knows to do it :)
