# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet restore
dotnet build -c Release

# Test (all)
dotnet test --no-build -c Release

# Test (single project)
dotnet test tests/<ProjectName>/<ProjectName>.csproj

# Pack (output to ./build/)
dotnet pack -c Release --no-build
```

`TreatWarningsAsErrors` is enabled in Release builds. Always build with `-c Release` before considering a session complete.

## Architecture

**AspNetCore.DataProtection.CustomStorage** is a layered library for storing ASP.NET Core Data Protection keys in custom backends.

### Layer Stack

```
ASP.NET Core Data Protection (IXmlRepository)
    └── StorageWrapper<TStorage>         [core: bridges IXmlRepository ↔ IDataProtectionStorage]
            └── IDataProtectionStorage   [core: GetAll() / Insert()]
                    └── IDbDataProtectionStorage  [Dapper: adds InitializeDb() / GetAllAsync()]
                            ├── SQLServerDataProtectionRepository
                            └── PostgreSQLDataProtectionRepository
```

### Key Types

| Type | Project | Role |
|------|---------|------|
| `IDataProtectionStorage` | Core | Storage contract |
| `StorageWrapper<TStorage>` | Core | `IXmlRepository` adapter; resolves `TStorage` from scoped `IServiceProvider` |
| `DataProtectionKey` | Core | `FriendlyName` + `Xml` |
| `DataProtectionKeyEntity` | Dapper | Extends `DataProtectionKey` with `Id` and `InsertDate` |
| `DapperDataProtectionConfig` | Dapper | Configuration record: schema, table, auto-init flag |
| `IDbDataProtectionStorage` | Dapper | Async-capable contract for DB-backed storage |

### Entry Points

- **Core:** `IDataProtectionBuilder.PersistKeysToStorage<TStorage>()` — registers `StorageWrapper<TStorage>` as `IConfigureOptions<KeyManagementOptions>`.
- **Dapper DB providers:** `DapperDataProtectionExtensions.UseDapperDataProtection()` — calls `InitializeDb()` on startup when `DapperDataProtectionConfig.InitializeTable = true`.

### Database Implementations

Both SQL Server and PostgreSQL repositories use raw SQL via Dapper. Schema/table names are configurable. SQL Server defaults to `dbo.DataProtectionKeys` with a NONCLUSTERED unique index on `FriendlyName`; PostgreSQL mirrors this structure.

### Testing

Tests use **xUnit**, **FluentAssertions**, and **NSubstitute**. Integration tests spin up real database containers via **Testcontainers** — no external database setup needed. **Respawn** resets state between tests; **Bogus** generates test data.

Core project exposes internals to the test project via `InternalsVisibleTo`.

### CI/CD

- `build.yml`: runs on push to `main` and PRs — restore → build (Release) → test.
- `nuget-publish.yml`: publishes to nuget.org on `v*` tags using `NUGET_KEY` secret.
- `github-release.yml`: creates GitHub release on `v*` tags.
