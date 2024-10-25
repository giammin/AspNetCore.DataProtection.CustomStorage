using System;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests;

public class DapperDataProtectionExtensionsTests
{
    [Fact]
    public void UseDapperDataProtection_InitializeTable_ExecInitializeDb()
    {
        var services = Substitute.For<IServiceProvider>();
        var provider = Substitute.For<IDbDataProtectionStorage>();
        var scope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var config = new DapperDataProtectionConfig()
        {
            SchemaName = "",
            TableName = "",
            InitializeTable = false
        };
        var options =
            Substitute.For<IOptions<DapperDataProtectionConfig>>();
        options.Value.Returns(config);

        services.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(services);
        services.GetService(typeof(IOptions<DapperDataProtectionConfig>)).Returns(options);
        services.GetService(typeof(IDataProtectionStorage)).Returns(provider);

        services.UseDapperDataProtection();

        provider.DidNotReceiveWithAnyArgs().InitializeDb();
    }
    [Fact]
    public void UseDapperDataProtection_InitializeTableFalse_NotInitializeDb()
    {
        //var serviceCollection = new ServiceCollection();
        //serviceCollection
        //    .AddDataProtection()
        //    .PersistKeysWithDapper();
        //var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
        //var keyManagementOptions = serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>();

        //Assert.IsAssignableFrom<IDbDataProtectionStorage>(keyManagementOptions.Value.XmlRepository);
        Assert.Fail("not implemented");
    }

}