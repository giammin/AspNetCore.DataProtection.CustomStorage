[![Build and test](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/actions/workflows/build.yml/badge.svg)](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/actions/workflows/build.yml)
[![Create Release](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/actions/workflows/github-release.yml/badge.svg)](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage/actions/workflows/github-release.yml)
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
- [Database schema](#database-schema)
- [Custom storage backend](#custom-storage-backend)
- [How it works](#how-it-works)
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

> **Note:** the PostgreSQL provider enables Dapper's snake_case column mapping globally
> (`Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true`). Keep this in mind if the same
> application uses Dapper elsewhere.

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

| Option | Type | Default (SQL Server) | Default (PostgreSQL) | Description |
|--------|------|----------------------|----------------------|-------------|
| `SchemaName` | `string` | `dbo` | `public` | Schema that contains the keys table. |
| `TableName` | `string` | `DataProtectionKeys` | `data_protection_keys` | Name of the keys table. |
| `InitializeTable` | `bool` | `true` | `true` | When `true`, `UseDapperDataProtection()` creates the table (and its unique index) if it is missing. |

`UseDapperDataProtection()` is an extension on `IServiceProvider`. Call it once at startup
(`app.Services.UseDapperDataProtection()`): it validates the service registrations and, when
`InitializeTable` is `true`, creates the table. If you set `InitializeTable = false` you are
responsible for creating the schema (see below) before the app stores keys.

## Database schema

When `InitializeTable` is enabled, the provider creates a table equivalent to the following.

SQL Server (`dbo.DataProtectionKeys` by default):

```sql
CREATE TABLE [dbo].[DataProtectionKeys](
    [Id]           [int] IDENTITY(1,1) NOT NULL,
    [InsertDate]   [datetime] NOT NULL DEFAULT getdate(),
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

- `FriendlyName` (`string?`) — a name that must be unique when it is not `null`.
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

## How it works

`PersistKeysToStorage<TStorage>()` registers a `StorageWrapper<TStorage>` as the Data Protection
`IXmlRepository`. The wrapper adapts the framework's XML-element calls to the simpler
`IDataProtectionStorage` contract (`GetAll()` / `Insert()`), resolving `TStorage` from a scoped
service provider for every operation.

The database providers add a Dapper layer (`IDbDataProtectionStorage`) on top of that contract,
implementing the raw SQL for SQL Server and PostgreSQL, plus schema initialization
(`InitializeDb()`) and an async read path (`GetAllAsync()`).

## Contributing

Issues and pull requests are welcome at the
[GitHub repository](https://github.com/giammin/AspNetCore.DataProtection.CustomStorage).

## License

Licensed under the [Apache License 2.0](LICENSE).
