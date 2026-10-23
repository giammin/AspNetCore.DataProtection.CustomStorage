[![Build and test](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/actions/workflows/build.yml/badge.svg)](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/actions/workflows/build.yml)
[![Publish Nuget package](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/actions/workflows/nuget-publish.yml/badge.svg)](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/actions/workflows/nuget-publish.yml)

[![GitHub release](https://img.shields.io/github/v/release/giammin/AspNetCore.DataProtection.CustomStorage)](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/releases)
[![GitHub issues](https://img.shields.io/github/issues/giammin/AspNetCore.DataProtection.CustomStorage)](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr-raw/giammin/AspNetCore.DataProtection.CustomStorage)](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/pulls)

| packages                                                 |                                                                                                                                                                                                                                           |
|----------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------| 
| AspNetCore.DataProtection.CustomStorage                  | [![AspNetCore.DataProtection.CustomStorage](https://img.shields.io/nuget/v/AspNetCore.DataProtection.CustomStorage)](https://www.nuget.org/packages/AspNetCore.DataProtection.CustomStorage/)                                             |
| AspNetCore.DataProtection.CustomStorage.Dapper           | [![AspNetCore.DataProtection.CustomStorage.Dapper](https://img.shields.io/nuget/v/AspNetCore.DataProtection.CustomStorage.Dapper)](https://www.nuget.org/packages/AspNetCore.DataProtection.CustomStorage.Dapper/)                        |
| AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer | [![AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer](https://img.shields.io/nuget/v/AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer)](https://www.nuget.org/packages/AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer/)                        |
| AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL | [![AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL](https://img.shields.io/nuget/v/AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL)](https://www.nuget.org/packages/AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL/) |


[![GitHub](https://img.shields.io/github/license/giammin/AspNetCore.DataProtection.CustomStorage)](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/blob/main/LICENSE)

# AspNetCore.DataProtection.CustomStorage

Persist [ASP.NET Core Data Protection](https://learn.microsoft.com/aspnet/core/security/data-protection/introduction) keys in a storage backend of your choice.

The base package lets you plug any backend into the Data Protection key ring by implementing a single, small interface. Ready-to-use SQL Server and PostgreSQL implementations (built on [Dapper](https://github.com/DapperLib/Dapper)) are provided as separate packages.

## Contents

- [Why](#why)
- [Packages](#packages)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
  - [SQL Server](#sql-server)
  - [PostgreSQL](#postgresql)
- [Configuration](#configuration)
  - [Schema and table names](#schema-and-table-names)
- [Database schema](#database-schema)
- [Custom storage backend](#custom-storage-backend)
- [Deleting keys](#deleting-keys)
- [How it works](#how-it-works)
- [Upgrading from v1 to v2](#upgrading-from-v1-to-v2)
- [Contributing](#contributing)
- [License](#license)

## Why

When an ASP.NET Core app runs on more than one node (containers, a web farm, autoscaling), every instance must share the same Data Protection keys; otherwise cookies, antiforgery tokens and anything else protected on one node cannot be read on another. This library keeps that key ring in a shared, durable store instead of the local filesystem.

## Packages

| Package | Description |
|---------|-------------|
| `AspNetCore.DataProtection.CustomStorage` | Core abstractions. Bring your own backend by implementing `IDataProtectionStorage`. |
| `AspNetCore.DataProtection.CustomStorage.Dapper` | Shared Dapper-based building blocks used by the database providers. |
| `AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer` | Ready-to-use SQL Server provider. |
| `AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL` | Ready-to-use PostgreSQL provider. |

The database provider packages transitively depend on `.Dapper` and the core package, so installing a provider is all you need for the SQL Server or PostgreSQL scenario.

## Requirements

- .NET 10.0 or later

## Installation

For SQL Server:

```bash
dotnet add package AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer
```

For PostgreSQL:

```bash
dotnet add package AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL
```

For a custom backend, install only the core package:

```bash
dotnet add package AspNetCore.DataProtection.CustomStorage
```

## Quick start

### SQL Server

```csharp
using AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
    .PersistKeysWithDapperInSqlServer(
        builder.Configuration.GetConnectionString("DataProtection")!);

var app = builder.Build();

// Creates the keys table if it does not exist yet
// (InitializeTable defaults to true; see Configuration below).
app.Services.UseDapperDataProtection();

app.Run();
```

The provider registers its `SqlConnectionFactory` as a keyed service. A `SqlConnectionFactory`
registered by the application is neither replaced nor used for the keys, whatever the registration
order.

### PostgreSQL

```csharp
using AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDataProtection()
    .PersistKeysWithDapperInPostgreSQL(
        builder.Configuration.GetConnectionString("DataProtection")!);

var app = builder.Build();

app.Services.UseDapperDataProtection();

app.Run();
```

The provider registers its `NpgsqlDataSource` as a keyed service and disposes it with the service
provider. An `NpgsqlDataSource` registered by the application (for example with
`AddNpgsqlDataSource`) is neither replaced nor used for the keys, whatever the registration order.

## Configuration

Both providers accept an optional `Action<DapperDataProtectionConfig>` to override the defaults:

```csharp
builder.Services.AddDataProtection()
    .PersistKeysWithDapperInSqlServer(connectionString, config =>
    {
        config.SchemaName = "security";
        config.TableName  = "DpKeys";
        config.InitializeTable = false; // you manage the schema yourself
    });
```

Call `PersistKeysWithDapperInSqlServer()` or `PersistKeysWithDapperInPostgreSQL()` only once: a
second call, to either provider, throws an `InvalidOperationException`.

| Option | Type | Default (SQL Server) | Default (PostgreSQL) | Description |
|--------|------|----------------------|----------------------|-------------|
| `SchemaName` | `string` | `dbo` | `public` | Schema that contains the keys table. See [Schema and table names](#schema-and-table-names). |
| `TableName` | `string` | `DataProtectionKeys` | `data_protection_keys` | Name of the keys table. See [Schema and table names](#schema-and-table-names). |
| `InitializeTable` | `bool` | `true` | `true` | When `true`, `UseDapperDataProtection()` creates the schema, the table and its unique index, each one only if it is missing. |

`UseDapperDataProtection()` is an extension on `IServiceProvider`. Call it once at startup
(`app.Services.UseDapperDataProtection()`): it validates the service registrations and, when
`InitializeTable` is `true`, creates the schema, the table and the friendly-name unique index if
they are missing. Each object is created only when it does not exist, so the database user needs the
permission to create it only in that case: once the schema, the table and the index exist, a user
that can only read, insert and delete rows of the table is enough. The index is also created on an existing table that lacks it; if that table already
contains duplicate non-null friendly names, initialization fails and the duplicates must be
removed before retrying. Initialization is safe when several instances start at the same time:
it runs in a transaction that holds a database-wide lock (`sp_getapplock` on SQL Server,
`pg_advisory_xact_lock` on PostgreSQL), so concurrent calls wait for each other. If you set
`InitializeTable = false` you are responsible for creating the schema (see below) before the
app stores keys.

### Schema and table names

`SchemaName` and `TableName` are validated when the host starts, and at the latest when
`UseDapperDataProtection()` runs. An invalid name throws an `OptionsValidationException` that names
the option and the rule:

| | SQL Server | PostgreSQL |
|---|---|---|
| Allowed characters | letters, digits and `_`; must not start with a digit | lowercase letters, digits and `_`; must not start with a digit |
| Maximum length | 128 for the schema, 112 for the table (room for the `IX_{TableName}_FriendlyName` index name) | 45 for the schema and the table together (room for the `ix_{SchemaName}_{TableName}_friendly_name` index name within the 63-character identifier limit) |

The names are always quoted in the generated SQL, so reserved words such as `order` or `user` are
allowed.

PostgreSQL folds unquoted names to lowercase, and earlier versions of this library did not quote
them. Quoted names are case-sensitive, so a mixed-case name such as `DpKeys` would now refer to a
new, empty table instead of the existing `dpkeys` table, and the existing keys would be ignored.
Mixed-case names are therefore rejected at startup: if you configured one, replace it with its
lowercase form, which is the name of the table created by the earlier version.

## Database schema

When `InitializeTable` is enabled, the provider creates a table equivalent to the following.

SQL Server (`dbo.DataProtectionKeys` by default):

```sql
CREATE TABLE [dbo].[DataProtectionKeys](
    [Id]           [int] IDENTITY(1,1) NOT NULL,
    [InsertDate]   [datetime2] NOT NULL CONSTRAINT [DF_DataProtectionKeys_InsertDate] DEFAULT sysutcdatetime(),
    [FriendlyName] [nvarchar](256) NULL,
    [Xml]          [nvarchar](max) NOT NULL,
    CONSTRAINT [PK_DataProtectionKeys] PRIMARY KEY CLUSTERED ([Id] ASC)
);
CREATE UNIQUE NONCLUSTERED INDEX [IX_DataProtectionKeys_FriendlyName]
    ON [dbo].[DataProtectionKeys]([FriendlyName] ASC)
    WHERE [FriendlyName] IS NOT NULL;
```

PostgreSQL (`public.data_protection_keys` by default):

```sql
CREATE TABLE IF NOT EXISTS public.data_protection_keys (
    id            INTEGER GENERATED ALWAYS AS IDENTITY,
    insert_date   timestamp with time zone NOT NULL DEFAULT NOW(),
    friendly_name character varying(256) NULL,
    xml           text NOT NULL,
    CONSTRAINT pk_public_data_protection_keys PRIMARY KEY (id)
);
CREATE UNIQUE INDEX IF NOT EXISTS ix_public_data_protection_keys_friendly_name
    ON public.data_protection_keys (friendly_name ASC NULLS LAST);
```

The unique index on `FriendlyName` / `friendly_name` allows `NULL` values.

`InsertDate` / `insert_date` is set by the database in UTC.

### Migrating an existing SQL Server table

Earlier versions created `[InsertDate]` as `[datetime] DEFAULT getdate()`, which stores the
server's local time with about 3 ms precision. `InitializeDb()` never alters an existing table,
so such a table keeps working unchanged. To align it with the current schema, run the following
optional script (adjust the schema and table names if you changed the defaults):

```sql
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- Time zone of the existing values. CURRENT_TIMEZONE_ID() requires SQL Server 2022 or
-- Azure SQL; on earlier versions set the server's Windows time zone name,
-- e.g. N'W. Europe Standard Time'.
DECLARE @timeZone sysname = CURRENT_TIMEZONE_ID();

-- Drop the existing default constraint, whose name was generated by SQL Server.
DECLARE @sql nvarchar(max) = (
    SELECT N'ALTER TABLE [dbo].[DataProtectionKeys] DROP CONSTRAINT ' + QUOTENAME(dc.[name])
    FROM [sys].[default_constraints] dc
      JOIN [sys].[columns] c ON c.[object_id] = dc.[parent_object_id] AND c.[column_id] = dc.[parent_column_id]
    WHERE dc.[parent_object_id] = OBJECT_ID(N'[dbo].[DataProtectionKeys]') AND c.[name] = N'InsertDate');
IF @sql IS NOT NULL EXEC sp_executesql @sql;

ALTER TABLE [dbo].[DataProtectionKeys] ALTER COLUMN [InsertDate] [datetime2] NOT NULL;

-- Convert the existing values from local time to UTC.
UPDATE [dbo].[DataProtectionKeys]
SET [InsertDate] = CONVERT(datetime2, [InsertDate] AT TIME ZONE @timeZone AT TIME ZONE 'UTC');

ALTER TABLE [dbo].[DataProtectionKeys]
    ADD CONSTRAINT [DF_DataProtectionKeys_InsertDate] DEFAULT sysutcdatetime() FOR [InsertDate];

COMMIT TRANSACTION;
```

## Custom storage backend

Reference `AspNetCore.DataProtection.CustomStorage` and implement `IDataProtectionStorage`:

```csharp
using AspNetCore.DataProtection.CustomStorage;

public class MyDataProtectionStorage : IDataProtectionStorage
{
    public IEnumerable<DataProtectionKey> GetAll()
    {
        // load every stored key from your backend
    }

    public void Insert(DataProtectionKey key)
    {
        // persist a new key; key.FriendlyName must be unique when it is not null
    }
}
```

`DataProtectionKey` carries the two values the key ring needs:

- `FriendlyName` (`string?`) — a name that must be unique when it is not `null`. Names are trimmed,
  and an empty or whitespace-only name is passed to `Insert` as `null`.
- `Xml` (`string`, required) — the serialized key.

Register your implementation in DI and wire it up:

```csharp
builder.Services.AddScoped<MyDataProtectionStorage>();

builder.Services.AddDataProtection()
    .PersistKeysToStorage<MyDataProtectionStorage>();
```

`PersistKeysToStorage<TStorage>()` resolves `TStorage` from a fresh dependency-injection scope
on each read/write, so you must register it yourself (any lifetime works). If an `Insert` throws,
it is wrapped in a `KeyInsertException`.

To support [key deletion](#deleting-keys), implement `IDeletableDataProtectionStorage` instead:

```csharp
public class MyDataProtectionStorage : IDeletableDataProtectionStorage
{
    // GetAll() and Insert() as above

    public void Delete(IReadOnlyCollection<DataProtectionKey> keys)
    {
        // delete every key, in the given order, or none of them (throw on failure)
    }
}
```

The keys passed to `Delete` are instances previously returned by `GetAll()` on the same storage
instance, so a backend can return a derived record that carries its own identifier (as the Dapper
providers do with `DataProtectionKeyEntity.Id`) and use it to locate the rows to delete.

## Deleting keys

Deleting a key makes the data it protected permanently unreadable; Microsoft
[recommends not deleting keys](https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/default-settings#delete-keys)
except in exceptional cases. When the storage supports it (both database providers do, as does any
`IDeletableDataProtectionStorage`), old keys can be deleted through the key manager:

```csharp
using Microsoft.AspNetCore.DataProtection.KeyManagement;

var keyManager = app.Services.GetRequiredService<IKeyManager>();
if (keyManager is XmlKeyManager { CanDeleteKeys: true } xmlKeyManager)
{
    var yearAgo = DateTimeOffset.UtcNow.AddYears(-1);
    if (!xmlKeyManager.DeleteKeys(key => key.ExpirationDate < yearAgo))
    {
        // the deletion failed and no key was deleted; see the ErrorDeletingKeysFromStorage log entry
    }
}
```

> **Note:** in ASP.NET Core 10.0, `XmlKeyManager` exposes `CanDeleteKeys` and `DeleteKeys` but does
> not implement `IDeletableKeyManager`, so a `keyManager is IDeletableKeyManager` check is `false`
> even when deletion is supported.

The selected keys are deleted with a single `Delete` call. The database providers delete the rows
by `Id`, in deletion order, in one transaction: either every selected key is deleted or none is.
If the storage throws, the error is logged (event `ErrorDeletingKeysFromStorage`) and `DeleteKeys`
returns `false`. Each deleted key is logged by friendly name (event `DeletingKeyFromStorage`); the
key XML is never logged. A stored key whose XML is malformed is not offered for deletion.

## How it works

`PersistKeysToStorage<TStorage>()` registers an internal `IXmlRepository` adapter as the Data
Protection key repository. The adapter maps the framework's XML-element calls to the simpler
`IDataProtectionStorage` contract (`GetAll()` / `Insert()`), resolving `TStorage` from a scoped
service provider for every operation. When `TStorage` implements `IDeletableDataProtectionStorage`,
the adapter also implements the framework's `IDeletableXmlRepository` and maps deletions to `Delete()`.

When the key ring is loaded, a stored key whose XML is malformed or missing is skipped and logged as an error
(event `SkippingMalformedKey`, with the friendly name and the parse position; the XML itself is never
logged). The remaining keys stay usable, but data protected with the skipped key can no longer be
unprotected, and if no valid key remains, Data Protection generates a new one.

The database providers add a Dapper layer (`IDbDataProtectionStorage`, which extends
`IDeletableDataProtectionStorage`) on top of that contract,
implementing the raw SQL for SQL Server and PostgreSQL, plus key deletion, schema initialization
(`InitializeDb()`) and an async read path (`GetAllAsync()`) that returns the stored
`DataProtectionKeyEntity` rows, including `Id` and `InsertDate`, for tooling such as key auditing.

## Upgrading from v1 to v2

Version 2 changes the public API, validates the configuration at startup and changes some behavior
of the providers. Check each item below against your application before upgrading from a 1.x
release.

### Breaking changes

#### `IDbDataProtectionStorage` requires `Delete`

Affects custom implementations of `IDbDataProtectionStorage`; applications that only use the SQL
Server or PostgreSQL provider are not affected.

`IDbDataProtectionStorage` now extends `IDeletableDataProtectionStorage`. Implement
`Delete(IReadOnlyCollection<DataProtectionKey> keys)`: delete every given key, in the given order,
or none of them, and throw on failure. The keys are instances previously returned by `GetAll()`.
See [Custom storage backend](#custom-storage-backend).

#### Schema and table names are validated at startup

Affects applications that set `SchemaName` or `TableName`.

An invalid name now fails when the host starts, or at the latest in `UseDapperDataProtection()`,
with an `OptionsValidationException`. The rules are listed in
[Schema and table names](#schema-and-table-names).

- **SQL Server:** v1 accepted any name, because it was enclosed in brackets. Names that start with
  a digit, contain characters other than letters, digits and `_`, or are longer than the limits
  must be changed: rename the table (for example with `sp_rename`) or move it to a valid schema
  (`ALTER SCHEMA ... TRANSFER`), then update the configuration.
- **PostgreSQL:** names must be lowercase. v1 did not quote the names, so PostgreSQL folded them to
  lowercase: a table configured as `DpKeys` was created as `dpkeys`. Replace the configured name
  with its lowercase form; the database does not need to change.
- **PostgreSQL:** the schema and table names together must be at most 45 characters long. v1
  accepted longer names, but PostgreSQL silently truncated the derived index name to 63
  characters, so two tables could end up with the same index name. Rename the table
  (`ALTER TABLE ... RENAME TO ...`) and its index to `ix_{SchemaName}_{TableName}_friendly_name`
  (`ALTER INDEX ... RENAME TO ...`), then update the configuration. If the index is not renamed,
  `InitializeDb()` creates a second unique index with the new name.

#### The PostgreSQL provider no longer enables Dapper's underscore mapping

Affects applications that run their own Dapper queries in the same process.

v1 set `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true` process-wide, which also changed
the column mapping of the application's own Dapper queries. v2 no longer touches this setting. If
your queries rely on it, set it at startup:

```csharp
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
```

#### The PostgreSQL provider registers its data source as a keyed service

v1 registered its `NpgsqlDataSource` as an unkeyed singleton. v2 registers it as a keyed service
(see [PostgreSQL](#postgresql)). This affects:

- Applications that resolve `NpgsqlDataSource` from dependency injection and received the keys
  data source: register your own data source, for example with `AddNpgsqlDataSource`.
- Applications that register `PostgreSQLDataProtectionRepository` themselves instead of calling
  `PersistKeysWithDapperInPostgreSQL()`: the constructor now resolves the provider's keyed data
  source. Call `PersistKeysWithDapperInPostgreSQL()`, or register the repository with a factory
  that passes the data source explicitly:

  ```csharp
  builder.Services.AddScoped<IDbDataProtectionStorage>(sp => new PostgreSQLDataProtectionRepository(
      dataSource, sp.GetRequiredService<IOptionsSnapshot<DapperDataProtectionConfig>>()));
  ```

A malformed connection string now throws when the data source is first resolved, in
`UseDapperDataProtection()`, instead of in `PersistKeysWithDapperInPostgreSQL()`.

#### The SQL Server provider registers its connection factory as a keyed service

v1 registered its `SqlConnectionFactory` as an unkeyed singleton. v2 registers it as a keyed service
(see [SQL Server](#sql-server)). This affects:

- Applications that resolve `SqlConnectionFactory` from dependency injection and received the keys
  connection factory: register your own `SqlConnectionFactory`.
- Applications that register `SQLServerDataProtectionRepository` themselves instead of calling
  `PersistKeysWithDapperInSqlServer()`: the constructor now resolves the provider's keyed connection
  factory. Call `PersistKeysWithDapperInSqlServer()`, or register the repository with a factory
  that passes the connection factory explicitly:

  ```csharp
  builder.Services.AddScoped<IDbDataProtectionStorage>(sp => new SQLServerDataProtectionRepository(
      new SqlConnectionFactory(connectionString),
      sp.GetRequiredService<IOptionsSnapshot<DapperDataProtectionConfig>>()));
  ```

#### The Dapper providers can be registered only once

v1 accepted several calls to `PersistKeysWithDapperInSqlServer()` and
`PersistKeysWithDapperInPostgreSQL()`: the last call silently replaced the storage of the previous
ones, which shared a single configuration. v2 throws an `InvalidOperationException` when an
`IDbDataProtectionStorage` is already registered. Keep a single call.

#### `StorageWrapper<TStorage>` is internal

Affects code that created `StorageWrapper<TStorage>` directly, for example to assign
`KeyManagementOptions.XmlRepository` by hand. Use `PersistKeysToStorage<TStorage>()` instead.

#### `LoggingExtensions` is internal

`AspNetCore.DataProtection.CustomStorage.LoggingExtensions` and `LogInitialization` are no longer
public. They were implementation details and have no replacement.

The event ids and names are unchanged. The messages of `ReadKeyFromStorage`, `SavingKeyToStorage`
and `ErrorSavingKeyToStorage` no longer contain a `Value` property (the key XML), and
`ErrorSavingKeyToStorage` now carries the storage exception. Update any log query or filter that
uses `Value`.

### Behavior changes

These need no code change, but can affect a running application:

- **SQL Server unique index on existing tables.** `InitializeDb()` now also creates the
  `IX_{TableName}_FriendlyName` index on an existing table that lacks it. If the table contains
  duplicate non-null friendly names, startup fails with an error naming the index. Find the
  duplicates with the query below, keep one row per name (rows with the same name normally hold
  the same key; compare their `Xml` before deleting), then start again:

  ```sql
  SELECT [FriendlyName], COUNT(*) AS [Rows]
  FROM [dbo].[DataProtectionKeys]
  WHERE [FriendlyName] IS NOT NULL
  GROUP BY [FriendlyName]
  HAVING COUNT(*) > 1;
  ```

- **SQL Server `InsertDate` in UTC.** New tables store `InsertDate` as `datetime2` in UTC. Existing
  tables are not altered and keep the server's local time; see
  [Migrating an existing SQL Server table](#migrating-an-existing-sql-server-table).
- **Missing schema created.** When `InitializeTable` is `true`, `InitializeDb()` creates a missing
  schema instead of failing. The database user needs the permission to create schemas only in
  that case.
- **PostgreSQL initialization with a restricted user.** `InitializeDb()` creates the table and the
  index only when they are missing. In v1, `CREATE TABLE IF NOT EXISTS` failed on every start for a
  user without the `CREATE` privilege on the schema, even when the table existed.
- **Blank friendly names stored as `NULL`.** An empty or whitespace-only friendly name is passed to
  `Insert` as `null`; v1 passed an empty string. Custom storages must accept `null`.
- **Malformed keys skipped.** A stored key whose XML is malformed or `null` is logged
  (`SkippingMalformedKey`) and skipped; in v1 it made loading the whole key ring fail.
- **No key XML in `KeyInsertException`.** The exception message names the key by friendly name
  only; the storage exception is the `InnerException`.
- **Key deletion enabled.** With the database providers, or any
  `IDeletableDataProtectionStorage`, `XmlKeyManager.CanDeleteKeys` is now `true` and
  `DeleteKeys` deletes the stored rows. See [Deleting keys](#deleting-keys).

### Packaging

- The packages depend on `Microsoft.AspNetCore.DataProtection` 10.0.13 or later. The direct
  reference to `System.Security.Cryptography.Xml` is removed; it is still referenced through
  `Microsoft.AspNetCore.DataProtection`.
- No separate symbols package is published: the PDB is embedded in the package, and Source Link
  points to the sources on GitHub.

## Contributing

Issues and pull requests are welcome at the
[GitHub repository](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage).

## License

Licensed under the [Apache License 2.0](LICENSE).
