using System;
using System.Collections.Generic;
using System.Linq;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using Constants = AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer.Constants;

namespace AspNetCore.DataProtection.CustomStorage.Tests.SQLServer;

/// <summary>
/// End-to-end tests that wire the SQL Server provider through the public extension methods and real DI.
/// </summary>
public class SqlServerEndToEndTests : IClassFixture<SqlServerContainerFixture>
{
    private const string ApplicationName = "AspNetCore.DataProtection.CustomStorage.Tests";
    private const string Purpose = "end-to-end";
    private readonly SqlServerContainerFixture _fixture;

    public SqlServerEndToEndTests(SqlServerContainerFixture fixture)
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

    private static string NewTableName() => $"Keys_{Guid.NewGuid():N}";

    private ServiceProvider BuildServiceProvider(string tableName)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysWithDapperInSqlServer(_fixture.ConnectionString, config => config.TableName = tableName);

        var provider = services.BuildServiceProvider();
        provider.UseDapperDataProtection();
        return provider;
    }

    private List<string?> GetStoredFriendlyNames(string tableName)
    {
        using var connection = _fixture.SqlConnectionFactory.CreateConnection();
        return connection.Query<string?>(
            $"SELECT [{nameof(DataProtectionKeyEntity.FriendlyName)}] FROM [{Constants.DefaultSchema}].[{tableName}];").AsList();
    }
}
