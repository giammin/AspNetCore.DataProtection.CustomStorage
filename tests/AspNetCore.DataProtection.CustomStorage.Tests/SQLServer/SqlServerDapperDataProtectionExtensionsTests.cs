using System;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests.SQLServer;

public class SqlServerDapperDataProtectionExtensionsTests
{
    private const string ConnectionString = "Server=localhost;Database=test";

    [Fact]
    public void PersistKeysWithDapperInSqlServer_DefaultConfig_PassesStartupValidation()
    {
        using var provider = BuildServiceProvider(_ => { });

        provider.GetRequiredService<IStartupValidator>().Invoking(x => x.Validate())
            .Should().NotThrow();
    }

    [Fact]
    public void PersistKeysWithDapperInSqlServer_InvalidTableName_FailsStartupValidation()
    {
        using var provider = BuildServiceProvider(config => config.TableName = "My-Keys");

        provider.GetRequiredService<IStartupValidator>().Invoking(x => x.Validate())
            .Should().Throw<OptionsValidationException>()
            .WithMessage($"*{nameof(DapperDataProtectionConfig)}.{nameof(DapperDataProtectionConfig.TableName)}*");
    }

    [Fact]
    public void UseDapperDataProtection_InvalidSchemaName_ThrowsBeforeTouchingTheDatabase()
    {
        using var provider = BuildServiceProvider(config => config.SchemaName = "");

        provider.Invoking(x => x.UseDapperDataProtection())
            .Should().Throw<OptionsValidationException>()
            .WithMessage($"*{nameof(DapperDataProtectionConfig)}.{nameof(DapperDataProtectionConfig.SchemaName)}*");
    }

    private static ServiceProvider BuildServiceProvider(Action<DapperDataProtectionConfig> configAction)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().PersistKeysWithDapperInSqlServer(ConnectionString, configAction);
        return services.BuildServiceProvider();
    }
}
