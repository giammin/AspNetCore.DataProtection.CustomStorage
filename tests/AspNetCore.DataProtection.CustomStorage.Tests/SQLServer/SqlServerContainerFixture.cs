using System;
using System.Threading.Tasks;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using NSubstitute;
using Respawn;
using Testcontainers.MsSql;
using Xunit;
using Constants = AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer.Constants;

namespace AspNetCore.DataProtection.CustomStorage.Tests.SQLServer;

/// <summary>
/// Shares one container among the SQLServer test classes. A collection fixture, unlike an assembly fixture,
/// is created only when tests of the collection are selected, so unit-only runs do not need Docker.
/// Tests that do not use the default table work on their own tables.
/// </summary>
[CollectionDefinition(nameof(SqlServerCollection))]
public class SqlServerCollection : ICollectionFixture<SqlServerContainerFixture>;
public class SqlServerContainerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU27-GDR1-ubuntu-22.04")
        .WithPassword(Guid.NewGuid().ToString())
        .Build();

    public string ConnectionString { get; private set; } = null!;
    public SqlConnectionFactory SqlConnectionFactory { get; private set; } = null!;
    public readonly IOptionsSnapshot<DapperDataProtectionConfig> Options =
            Substitute.For<IOptionsSnapshot<DapperDataProtectionConfig>>();

    private Respawner _respawner = null!;

    /// <summary>
    /// Returns a unique table name, for tests that must not share the default table.
    /// </summary>
    public static string NewTableName() => $"Keys_{Guid.NewGuid():N}";

    public SqlServerContainerFixture()
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

        var initialCatalog = Guid.NewGuid().ToString("D");
        await _dbContainer.ExecScriptAsync($"create database [{initialCatalog}]");
        var connectionStringBuilder = new SqlConnectionStringBuilder(_dbContainer.GetConnectionString())
        {
            InitialCatalog = initialCatalog,
        };

        ConnectionString = connectionStringBuilder.ConnectionString;
        SqlConnectionFactory = new SqlConnectionFactory(ConnectionString);

        SeedDatabase();
        await using var dbConnection = SqlConnectionFactory.CreateConnection();
        await dbConnection.OpenAsync();
        
        _respawner = await Respawner.CreateAsync(dbConnection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.SqlServer,
                SchemasToInclude = [Constants.DefaultSchema]
            });
    }

    private void SeedDatabase()
    {
        var repo = new SQLServerDataProtectionRepository(SqlConnectionFactory, Options);
        repo.InitializeDb();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var dbConnection = SqlConnectionFactory.CreateConnection();
        await dbConnection.OpenAsync();
        await _respawner.ResetAsync(dbConnection);
    }

    public async ValueTask DisposeAsync()
    {
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }
}