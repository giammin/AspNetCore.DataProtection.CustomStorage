using System;
using System.Collections.Generic;
using System.Linq;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests.PostgreSQL;

/// <summary>
/// End-to-end tests that wire the PostgreSQL provider through the public extension methods and real DI.
/// </summary>
public class PostgreSqlEndToEndTests : IClassFixture<PostgreSqlContainerFixture>
{
    private const string ApplicationName = "AspNetCore.DataProtection.CustomStorage.Tests";
    private const string Purpose = "end-to-end";
    private readonly PostgreSqlContainerFixture _fixture;

    public PostgreSqlEndToEndTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Protect_UnprotectWithSameServiceProvider_RoundTrips()
    {
        var tableName = NewTableName();
        using var services = BuildServiceProvider(tableName);
        var protector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose);

        var protectedPayload = protector.Protect("payload");

        protector.Unprotect(protectedPayload).Should().Be("payload");
        GetStoredFriendlyNames(tableName).Should().ContainSingle().Which.Should().StartWith("key-");
    }

    [Fact]
    public void Protect_UnprotectWithSecondServiceProviderSharingDatabase_RoundTrips()
    {
        var tableName = NewTableName();
        using var first = BuildServiceProvider(tableName);
        using var second = BuildServiceProvider(tableName);

        var protectedPayload = first.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(Purpose).Protect("payload");

        second.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector(Purpose).Unprotect(protectedPayload).Should().Be("payload");
        GetStoredFriendlyNames(tableName).Should().ContainSingle();
    }

    [Fact]
    public void DeleteKeys_KeySelected_DeletesItFromTable()
    {
        var tableName = NewTableName();
        using var services = BuildServiceProvider(tableName);
        // XmlKeyManager exposes DeleteKeys but does not implement IDeletableKeyManager.
        var keyManager = (XmlKeyManager)services.GetRequiredService<IKeyManager>();
        var now = DateTimeOffset.UtcNow;
        var kept = keyManager.CreateNewKey(now, now.AddDays(90));
        var deleted = keyManager.CreateNewKey(now, now.AddDays(90));

        keyManager.DeleteKeys(key => key.KeyId == deleted.KeyId).Should().BeTrue();

        keyManager.GetAllKeys().Select(k => k.KeyId).Should().Equal(kept.KeyId);
        GetStoredFriendlyNames(tableName).Should().Equal($"key-{kept.KeyId:D}");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Protect_ApplicationRegistersOwnDataSource_DataSourcesStaySeparate(bool applicationRegistersFirst)
    {
        var tableName = NewTableName();
        // The application data source targets a database that does not exist, so that any key
        // operation routed through it fails.
        var applicationConnectionString = new NpgsqlConnectionStringBuilder(_fixture.ConnectionString)
        {
            Database = "application_database_does_not_exist"
        }.ConnectionString;
        using var applicationDataSource = NpgsqlDataSource.Create(applicationConnectionString);
        using var services = BuildServiceProvider(tableName, applicationDataSource, applicationRegistersFirst);
        var protector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector(Purpose);

        var protectedPayload = protector.Protect("payload");

        services.GetRequiredService<NpgsqlDataSource>().Should().BeSameAs(applicationDataSource);
        protector.Unprotect(protectedPayload).Should().Be("payload");
        GetStoredFriendlyNames(tableName).Should().ContainSingle();
    }

    // PostgreSQL folds unquoted identifiers to lower case; keep the name short enough for derived index names.
    private static string NewTableName() => $"keys_{Guid.NewGuid().ToString("N")[..12]}";

    private ServiceProvider BuildServiceProvider(string tableName,
        NpgsqlDataSource? applicationDataSource = null,
        bool applicationRegistersFirst = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        if (applicationDataSource is not null && applicationRegistersFirst)
        {
            services.AddSingleton(applicationDataSource);
        }
        services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysWithDapperInPostgreSQL(_fixture.ConnectionString, config => config.TableName = tableName);
        if (applicationDataSource is not null && !applicationRegistersFirst)
        {
            services.AddSingleton(applicationDataSource);
        }

        var provider = services.BuildServiceProvider();
        provider.UseDapperDataProtection();
        return provider;
    }

    private List<string?> GetStoredFriendlyNames(string tableName)
    {
        using var connection = _fixture.NpgsqlDataSource.OpenConnection();
        return connection.Query<string?>(
            $"SELECT friendly_name FROM {Constants.DefaultSchema}.{tableName};").AsList();
    }
}
