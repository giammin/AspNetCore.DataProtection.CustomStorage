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

## Build Configuration

- Solution file is `AspNetCore.DataProtection.CustomStorage.slnx`. Target framework: `net10.0`.
- Central package management: versions live in `Directory.Packages.props`; `<PackageReference>` items have no `Version`.
- `src/Directory.Build.props` holds shared package metadata and a separate `VersionPrefix*` property per package. CI overrides the version with `-p:Version=<tag>` on release.
- `ImplicitUsings` is disabled everywhere: add explicit `using` directives. `Nullable` is enabled.
- `InternalsVisibleTo AspNetCore.DataProtection.CustomStorage.Tests` is applied to all `src/` projects through `src/Directory.Build.props`.

## Architecture

**AspNetCore.DataProtection.CustomStorage** is a layered library for storing ASP.NET Core Data Protection keys in custom backends. Each `src/` project is a separate NuGet package; each layer references only the one below it.

```
ASP.NET Core Data Protection (IXmlRepository)
    └── StorageWrapper<TStorage>         [Core: bridges IXmlRepository <-> IDataProtectionStorage]
            └── IDataProtectionStorage   [Core: GetAll() / Insert()]
                    └── IDbDataProtectionStorage  [Dapper: adds InitializeDb() / GetAllAsync()]
                            ├── SQLServerDataProtectionRepository   [Dapper.SQLServer]
                            └── PostgreSQLDataProtectionRepository  [Dapper.PostgreSQL]
```

### Wiring

- **Core:** `PersistKeysToStorage<TStorage>()` registers a singleton `IConfigureOptions<KeyManagementOptions>` that sets `XmlRepository` to a `StorageWrapper<TStorage>`. It does **not** register `TStorage`; the caller must.
- `StorageWrapper` creates a new DI scope and resolves `TStorage` on every `GetAllElements` / `StoreElement` call, so scoped storage implementations work. It trims `FriendlyName` and wraps any `Insert` exception in `KeyInsertException`.
- **DB providers:** `PersistKeysWithDapperInSqlServer(...)` / `PersistKeysWithDapperInPostgreSQL(...)` configure `IOptions<DapperDataProtectionConfig>`, register the connection source as a singleton (`SqlConnectionFactory` / `NpgsqlDataSource`), register the repository as scoped `IDbDataProtectionStorage`, then call `PersistKeysToStorage<IDbDataProtectionStorage>()`.
- `IServiceProvider.UseDapperDataProtection()` (in the shared Dapper project) must be called at startup. It resolves the registrations and runs `InitializeDb()` when `DapperDataProtectionConfig.InitializeTable` is `true` (the default).

### Database Implementations

Both repositories use raw SQL via Dapper. They differ in naming conventions:

| | SQL Server | PostgreSQL |
|---|---|---|
| Default table | `dbo.DataProtectionKeys` | `public.data_protection_keys` |
| Columns | PascalCase, via `nameof(DataProtectionKeyEntity.X)` | snake_case literals |
| Unique index on friendly name | Filtered NONCLUSTERED (`WHERE ... IS NOT NULL`) | `CREATE UNIQUE INDEX IF NOT EXISTS` (NULLs allowed) |

- The PostgreSQL provider sets the global `Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true` so snake_case columns map to `DataProtectionKeyEntity`. This affects the whole host process (documented in the README).
- Schema and table names are interpolated directly into SQL strings, not parameterized or validated.
- Defaults are in each provider's `Constants.cs`. Keep the table DDL in sync with the "Database schema" section of `README.md`.

### Testing

Single test project: `tests/AspNetCore.DataProtection.CustomStorage.Tests` (xUnit v3, FluentAssertions, NSubstitute, Bogus).

- `StorageWrapperTests` and `DapperDataProtectionExtensionsTests` are pure unit tests.
- `SQLServer/` and `PostgreSQL/` tests use an `IClassFixture` (`*ContainerFixture`) that starts a Testcontainers database, calls `InitializeDb()` with the default config, and exposes `ResetDatabaseAsync()` (Respawn) to clear data between tests.

### CI/CD (`.github/workflows`)

- `build.yml`: push to `main` and PRs: restore, build (Release), test.
- `nuget-publish.yml`: on `v*` tags, builds with `-p:Version=${tag without v}`, tests, obtains a short-lived key via NuGet trusted publishing (`NuGet/login`, OIDC, `NUGET_USER` secret) and pushes `./build/*.symbols.nupkg`.
- `github-release.yml`: creates a GitHub release on `v*` tags (`marvinpinto/action-automatic-releases`), with a changelog generated from the Conventional Commit messages on `main` since the previous tag.

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
