# AGENTS.md

This file provides guidance to AI coding agents working with code in this repository.

## Commands

```bash
# Build (Release also packs every src project into ./build/ via GeneratePackageOnBuild)
dotnet restore
dotnet build -c Release

# Test (all) - requires a running Docker daemon (Testcontainers)
dotnet test --no-build -c Release

# Test (single class / method) - xUnit v3 on Microsoft Testing Platform
dotnet test --no-build -c Release --filter-class "*.StorageWrapperTests"
dotnet test --no-build -c Release --filter-method "*.StorageWrapperTests.StoreElement*"
```

- `global.json` switches `dotnet test` to the native Microsoft Testing Platform (MTP) mode (`"test": { "runner": "Microsoft.Testing.Platform" }`). This is required: xUnit v3 4.x ships MTP 2.x, which no longer runs under the legacy VSTest mode on the .NET 10 SDK. In MTP mode, select targets with `--project` / `--solution` instead of a positional path.
- `TreatWarningsAsErrors` is enabled in Release builds for `src/`. Combined with `GenerateDocumentationFile`, every public member needs an XML doc comment or the Release build fails. Always build with `-c Release` before considering a session complete.
- SQL Server and PostgreSQL tests start real containers; the first run pulls images and is slow.
- On Windows with a Docker engine running inside WSL2 (no Docker Desktop), `localhost` resolves to `::1` first and SQL Server connections time out. Set `TESTCONTAINERS_HOST_OVERRIDE=127.0.0.1` (or `host.override=127.0.0.1` in `~/.testcontainers.properties`) instead of patching connection strings in the fixtures.

## Build Configuration

- Solution file is `AspNetCore.DataProtection.CustomStorage.slnx`. Target framework: `net10.0`.
- Central package management: versions live in `Directory.Packages.props`; `<PackageReference>` items have no `Version`.
- `src/Directory.Build.props` holds shared package metadata and a separate `VersionPrefix*` property per package. CI overrides the version with `-p:Version=<tag>` on release.
- `ImplicitUsings` is disabled everywhere: add explicit `using` directives. `Nullable` is enabled.
- Packages embed the PDB (`DebugType=embedded`) and rely on Source Link; no separate symbols package is produced. `ContinuousIntegrationBuild` is enabled only on GitHub Actions (`GITHUB_ACTIONS=true`).
- `InternalsVisibleTo AspNetCore.DataProtection.CustomStorage.Tests` is applied to all `src/` projects through `src/Directory.Build.props`.

## Architecture

**AspNetCore.DataProtection.CustomStorage** is a layered library for storing ASP.NET Core Data Protection keys in custom backends. Each `src/` project is a separate NuGet package; each layer references only the one below it.

```
ASP.NET Core Data Protection (IXmlRepository)
    └── StorageWrapper<TStorage>         [Core: bridges IXmlRepository <-> IDataProtectionStorage]
        (DeletableStorageWrapper<TStorage> also bridges IDeletableXmlRepository <-> IDeletableDataProtectionStorage)
            └── IDataProtectionStorage   [Core: GetAll() / Insert()]
                └── IDeletableDataProtectionStorage  [Core: adds Delete()]
                    └── IDbDataProtectionStorage  [Dapper: adds InitializeDb() / GetAllAsync()]
                            ├── SQLServerDataProtectionRepository   [Dapper.SQLServer]
                            └── PostgreSQLDataProtectionRepository  [Dapper.PostgreSQL]
```

### Wiring

- **Core:** `PersistKeysToStorage<TStorage>()` registers a singleton `IConfigureOptions<KeyManagementOptions>` that sets `XmlRepository` to a `StorageWrapper<TStorage>`, or to a `DeletableStorageWrapper<TStorage>` when `TStorage` is assignable to `IDeletableDataProtectionStorage` (so `XmlKeyManager.CanDeleteKeys` reflects the storage). It does **not** register `TStorage`; the caller must.
- `DeletableStorageWrapper.DeleteElements` offers the parsed keys (malformed rows skipped), then passes the chosen `GetAll()` instances, ordered by `DeletionOrder`, to a single `Delete()` call. A storage exception is logged (`ErrorDeletingKeysFromStorage`) and returns `false`. Note: in ASP.NET Core 10.0, `XmlKeyManager` exposes `DeleteKeys` but does not implement `IDeletableKeyManager`.
- `StorageWrapper` (internal) creates a new DI scope and resolves `TStorage` on every `GetAllElements` / `StoreElement` call, so scoped storage implementations work. It trims `FriendlyName`, normalizes empty or whitespace-only names to `null`, and wraps any `Insert` exception in `KeyInsertException`. `GetAllElements` logs and skips rows with malformed XML (`SkippingMalformedKey`) instead of failing the whole key ring.
- Never log or expose key XML (it may contain unencrypted key material); this includes `XmlException` messages, which can quote the input.
- **DB providers:** `PersistKeysWithDapperInSqlServer(...)` / `PersistKeysWithDapperInPostgreSQL(...)` configure `IOptions<DapperDataProtectionConfig>` with an internal per-provider `DapperDataProtectionConfigValidator` and `ValidateOnStart()`, register the connection source as a keyed singleton, so it never collides with one registered by the application (`SqlConnectionFactory` under the internal `Constants.ConnectionFactoryServiceKey`; `NpgsqlDataSource` under the internal `Constants.DataSourceServiceKey`, created by a factory so the container disposes it), register the repository as scoped `IDbDataProtectionStorage`, then call `PersistKeysToStorage<IDbDataProtectionStorage>()`.
- `IServiceProvider.UseDapperDataProtection()` (in the shared Dapper project) must be called at startup. It resolves the registrations and runs `InitializeDb()` when `DapperDataProtectionConfig.InitializeTable` is `true` (the default). Reading the options there also triggers validation when no host runs `ValidateOnStart`.

### Database Implementations

Both repositories use raw SQL via Dapper. They differ in naming conventions:

| | SQL Server | PostgreSQL |
|---|---|---|
| Default table | `dbo.DataProtectionKeys` | `public.data_protection_keys` |
| Columns | PascalCase, via `nameof(DataProtectionKeyEntity.X)` | snake_case literals |
| Unique index on friendly name | Filtered NONCLUSTERED (`WHERE ... IS NOT NULL`) | `CREATE UNIQUE INDEX IF NOT EXISTS` (NULLs allowed) |

- The PostgreSQL `SELECT` aliases each snake_case column to its `DataProtectionKeyEntity` property name (`AS "{nameof(...)}"`). Never set the process-wide `Dapper.DefaultTypeMap.MatchNamesWithUnderscores`: it changes column mapping for every Dapper query in the host application.
- `Delete()` requires `DataProtectionKeyEntity` instances (as returned by `GetAll()`) and deletes by `Id`, row by row in order, in one transaction.
- Schema and table names are validated at startup (SQL Server: `^[A-Za-z_][A-Za-z0-9_]*`, schema <= 128, table <= 112 to fit `IX_{table}_FriendlyName`; PostgreSQL: lowercase only, schema + table <= 45 to fit `ix_{schema}_{table}_friendly_name` in 63) and always quoted (`[...]` / `"..."`, with the closing quote escaped). PostgreSQL names are lowercase because earlier versions did not quote them: a mixed-case name would silently target a new, empty table.
- SQL Server passes names as parameters wherever T-SQL accepts them (existence checks, `QUOTENAME`, the duplicate-index error message).
- `InitializeDb()` creates the schema, the table and the index only when missing. On PostgreSQL it checks existence first (`pg_namespace`, `pg_tables`, `pg_indexes`), because PostgreSQL checks privileges before `IF NOT EXISTS`: a user without CREATE on the database or schema, or without ownership of the table, would otherwise fail on every start.
- Defaults are in each provider's `Constants.cs`. Keep the table DDL in sync with the "Database schema" section of `README.md`.

### Testing

Single test project: `tests/AspNetCore.DataProtection.CustomStorage.Tests` (xUnit v3, FluentAssertions, NSubstitute, Bogus).

- `StorageWrapperTests`, `DeletableStorageWrapperTests` and `DapperDataProtectionExtensionsTests` are pure unit tests. `DataProtectionStorageExtensionsTests` runs the real DI container and `XmlKeyManager` against in-memory storages.
- `SQLServer/` and `PostgreSQL/` tests share one xUnit collection fixture per provider (`*ContainerFixture` via `[Collection(nameof(*Collection))]`; not an assembly fixture, which xUnit v3 starts on every run, even unit-only ones) that starts a Testcontainers database, calls `InitializeDb()` with the default config, and exposes `ResetDatabaseAsync()` (Respawn) to clear the default table between tests. Respawn captures its table list at fixture start, so tests that create their own tables are unaffected; any test that does not reset must use its own table (`*ContainerFixture.NewTableName()`).
- `*EndToEndTests` wire each provider through the public extension methods and a real DI container (`AddDataProtection().PersistKeysWithDapperIn...()`, `UseDapperDataProtection()`), then protect/unprotect, including across two service providers sharing one table, and delete keys through `XmlKeyManager.DeleteKeys`. Each test uses its own table, so it does not depend on Respawn.

### CI/CD (`.github/workflows`)

- `build.yml`: push to `main` and PRs: restore, build (Release), test.
- `nuget-publish.yml`: on `v*` tags, two jobs:
  - `build`: builds with `-p:Version=${tag without v}`, tests, obtains a short-lived key via NuGet trusted publishing (`NuGet/login`, OIDC, `NUGET_USER` secret) and pushes `./build/*.nupkg`.
  - `release` (`needs: build`, so it runs only after a successful publish): generates the changelog with an inline shell script that groups the commit subjects since the previous `v*` tag by Conventional Commit type (plus a "Breaking Changes" section for `!` or a `BREAKING CHANGE:` footer), then runs `gh release create`.
- Every action is pinned to a full commit SHA with the version as a trailing comment (`uses: owner/repo@<sha> # vX.Y.Z`). Keep that format when updating an action.
- Workflows declare least-privilege `permissions:` explicitly.

## Commits and Pull Requests

Pull requests are squash-merged. The squash commit on `main` is the input of the release changelog, so its message must follow [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/).

- **Subject.** The repository uses the `COMMIT_OR_PR_TITLE` squash setting: a single-commit PR uses the commit subject, a multi-commit PR uses the PR title. Both every commit subject and the PR title must therefore be a Conventional Commit header:
  `<type>[(<scope>)][!]: <description>`
  - `type`: `feat`, `fix`, `perf`, `refactor`, `docs`, `test`, `build`, `ci`, `chore`, `revert`.
  - `scope` (optional): `core`, `dapper`, `sqlserver`, `postgresql`, `ci`, `deps`.
  - `description`: imperative mood, lowercase first letter, no trailing period. Keep the whole subject within 72 characters, including the ` (#<PR>)` suffix GitHub appends.
  - Never use a branch name as PR title (e.g. `Chore/update packages 2026 10`): it cannot be classified in the changelog.
- **Body.** GitHub pre-fills the squash body with the list of branch commits. Replace it with a short summary of the change, followed by a blank line and the footers:
  - `Closes #<issue>` for each resolved issue.
  - `BREAKING CHANGE: <what breaks and how to migrate>` for breaking changes to the public API or the database schema. Add it even when the subject uses `!`.
- **Merging from the CLI**, set the message explicitly instead of relying on the defaults:

  ```bash
  gh pr merge <PR> --squash \
    --subject "fix(sqlserver): ensure friendly-name unique index on existing tables (#<PR>)" \
    --body "$(printf '<summary>\n\nCloses #<issue>')"
  ```
