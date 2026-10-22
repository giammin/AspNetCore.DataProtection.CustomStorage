using System;
using System.Threading.Tasks;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests.PostgreSQL;

/// <summary>
/// Shares one container among the PostgreSQL test classes. A collection fixture, unlike an assembly fixture,
/// is created only when tests of the collection are selected, so unit-only runs do not need Docker.
/// Tests that do not use the default table work on their own tables.
/// </summary>
[CollectionDefinition(nameof(PostgreSqlCollection))]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlContainerFixture>;

public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:16.4")
        .WithWaitStrategy(Wait.ForUnixContainer())
        .Build();

    public string ConnectionString => _dbContainer.GetConnectionString();
    public NpgsqlDataSource NpgsqlDataSource { get; private set; } = null!;
    public readonly IOptionsSnapshot<DapperDataProtectionConfig> Options =
            Substitute.For<IOptionsSnapshot<DapperDataProtectionConfig>>();

    private Respawner _respawner = null!;

    /// <summary>
    /// Returns a unique table name, for tests that must not share the default table.
    /// </summary>
    public static string NewTableName() => $"keys_{Guid.NewGuid().ToString("N")[..12]}";

    public PostgreSqlContainerFixture()
    {
        Options.Value.Returns(new DapperDataProtectionConfig()
        {
            SchemaName = Constants.DefaultSchema,
            TableName = Constants.TableName
        });
    }

    public async ValueTask InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var npgsqlDataSourceBuilder = new NpgsqlDataSourceBuilder(_dbContainer.GetConnectionString())
        {
            ConnectionStringBuilder =
            {
                IncludeErrorDetail = true
            }
        };
        NpgsqlDataSource = npgsqlDataSourceBuilder.Build();
        SeedDatabase();
        await using var dbConnection = await NpgsqlDataSource.OpenConnectionAsync();
        
        _respawner = await Respawner.CreateAsync(dbConnection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = [Constants.DefaultSchema]
            });
    }

    private void SeedDatabase()
    {
        var repo = new PostgreSQLDataProtectionRepository(NpgsqlDataSource, Options);
        repo.InitializeDb();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var dbConnection = await NpgsqlDataSource.OpenConnectionAsync();
        await _respawner.ResetAsync(dbConnection);
    }

    public async ValueTask DisposeAsync()
    {
        await NpgsqlDataSource.DisposeAsync();
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }
}