using System;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests.PostgreSQL;

public class PostgreSqlDapperDataProtectionExtensionsTests
{
    [Theory]
    [InlineData("Host=localhost;Database=test", false)]
    [InlineData("Host=localhost;Database=test;Include Error Detail=false", false)]
    [InlineData("Host=localhost;Database=test;Include Error Detail=true", true)]
    public void PersistKeysWithDapperInPostgreSQL_PreservesIncludeErrorDetail(string connectionString, bool expected)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().PersistKeysWithDapperInPostgreSQL(connectionString);
        using var provider = services.BuildServiceProvider();

        var dataSource = provider.GetRequiredKeyedService<NpgsqlDataSource>(Constants.DataSourceServiceKey);

        new NpgsqlConnectionStringBuilder(dataSource.ConnectionString).IncludeErrorDetail
            .Should().Be(expected);
    }

    [Fact]
    public void PersistKeysWithDapperInPostgreSQL_DoesNotRegisterUnkeyedDataSource()
    {
        var services = new ServiceCollection();

        services.AddDataProtection().PersistKeysWithDapperInPostgreSQL("Host=localhost;Database=test");

        services.Should().NotContain(d => d.ServiceType == typeof(NpgsqlDataSource) && !d.IsKeyedService);
    }

    [Fact]
    public void PersistKeysWithDapperInPostgreSQL_ServiceProviderDisposed_DisposesDataSource()
    {
        var services = new ServiceCollection();
        services.AddDataProtection().PersistKeysWithDapperInPostgreSQL("Host=localhost;Database=test");
        var provider = services.BuildServiceProvider();
        var dataSource = provider.GetRequiredKeyedService<NpgsqlDataSource>(Constants.DataSourceServiceKey);

        provider.Dispose();

        dataSource.Invoking(d => d.OpenConnection()).Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void PersistKeysWithDapperInPostgreSQL_DoesNotEnableMatchNamesWithUnderscores()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = false;
        var services = new ServiceCollection();

        services.AddDataProtection().PersistKeysWithDapperInPostgreSQL("Host=localhost;Database=test");

        DefaultTypeMap.MatchNamesWithUnderscores.Should().BeFalse();
    }

    [Fact]
    public void PersistKeysWithDapperInPostgreSQL_CalledTwice_ThrowsInvalidOperationException()
    {
        var builder = new ServiceCollection().AddDataProtection().PersistKeysWithDapperInPostgreSQL("Host=localhost;Database=test");

        builder.Invoking(x => x.PersistKeysWithDapperInPostgreSQL("Host=localhost;Database=test"))
            .Should().Throw<InvalidOperationException>()
            .WithMessage($"*{nameof(IDbDataProtectionStorage)}*");
    }

    [Fact]
    public void PersistKeysWithDapperInPostgreSQL_AfterSqlServerProvider_ThrowsInvalidOperationException()
    {
        var builder = new ServiceCollection().AddDataProtection();
        Dapper.SQLServer.DapperDataProtectionExtensions.PersistKeysWithDapperInSqlServer(builder, "Server=localhost;Database=test");

        builder.Invoking(x => x.PersistKeysWithDapperInPostgreSQL("Host=localhost;Database=test"))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void PersistKeysWithDapperInPostgreSQL_DefaultConfig_PassesStartupValidation()
    {
        using var provider = BuildServiceProvider(_ => { });

        provider.GetRequiredService<IStartupValidator>().Invoking(x => x.Validate())
            .Should().NotThrow();
    }

    [Fact]
    public void PersistKeysWithDapperInPostgreSQL_MixedCaseTableName_FailsStartupValidation()
    {
        using var provider = BuildServiceProvider(config => config.TableName = "DataProtectionKeys");

        provider.GetRequiredService<IStartupValidator>().Invoking(x => x.Validate())
            .Should().Throw<OptionsValidationException>()
            .WithMessage($"*{nameof(DapperDataProtectionConfig)}.{nameof(DapperDataProtectionConfig.TableName)}*");
    }

    [Fact]
    public void UseDapperDataProtection_InvalidSchemaName_ThrowsBeforeTouchingTheDatabase()
    {
        using var provider = BuildServiceProvider(config => config.SchemaName = "my-schema");

        provider.Invoking(x => x.UseDapperDataProtection())
            .Should().Throw<OptionsValidationException>()
            .WithMessage($"*{nameof(DapperDataProtectionConfig)}.{nameof(DapperDataProtectionConfig.SchemaName)}*");
    }

    private static ServiceProvider BuildServiceProvider(Action<DapperDataProtectionConfig> configAction)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().PersistKeysWithDapperInPostgreSQL("Host=localhost;Database=test", configAction);
        return services.BuildServiceProvider();
    }
}
