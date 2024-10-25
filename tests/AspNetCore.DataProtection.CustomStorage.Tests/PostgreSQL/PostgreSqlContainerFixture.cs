using System.Threading.Tasks;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;
using Dapper;
using DotNet.Testcontainers.Builders;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;
using Respawn;
using Testcontainers.PostgreSql;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests.PostgreSQL;
public class PostgreSqlContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16.3")
        .WithWaitStrategy(Wait.ForUnixContainer())
        .Build();

    public NpgsqlDataSource NpgsqlDataSource { get; private set; } = null!;
    public readonly IOptionsSnapshot<DapperDataProtectionConfig> Options =
            Substitute.For<IOptionsSnapshot<DapperDataProtectionConfig>>();

    private Respawner _respawner = null!;

    public PostgreSqlContainerFixture()
    {
        Options.Value.Returns(new DapperDataProtectionConfig()
        {
            SchemaName = Constants.DefaultSchema,
            TableName = Constants.TableName
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        var npgsqlDataSourceBuilder = new NpgsqlDataSourceBuilder(_dbContainer.GetConnectionString())
        {
            ConnectionStringBuilder =
            {
                IncludeErrorDetail = true
            }
        };
#if DEBUG
        npgsqlDataSourceBuilder.ConnectionStringBuilder.IncludeErrorDetail = true;
#endif
        NpgsqlDataSource = npgsqlDataSourceBuilder.Build();
        
        //dapper
        DefaultTypeMap.MatchNamesWithUnderscores = true;
        SeedDatabase();
        await using var dbConnection = await NpgsqlDataSource.OpenConnectionAsync();
        
        _respawner = await Respawner.CreateAsync(dbConnection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"]
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

    public async Task DisposeAsync()
    {
        await NpgsqlDataSource.DisposeAsync();
        await _dbContainer.StopAsync();
        await _dbContainer.DisposeAsync();
    }
}