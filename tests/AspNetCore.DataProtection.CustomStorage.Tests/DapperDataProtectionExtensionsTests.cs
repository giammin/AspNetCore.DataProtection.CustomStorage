using System;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests;

public class DapperDataProtectionExtensionsTests
{
    private readonly IServiceProvider _services;
    private readonly IOptions<DapperDataProtectionConfig> _options;
    private readonly IDbDataProtectionStorage _provider;

    public DapperDataProtectionExtensionsTests()
    {
        _services = Substitute.For<IServiceProvider>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        _provider = Substitute.For<IDbDataProtectionStorage>();
        var scope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        _options = Substitute.For<IOptions<DapperDataProtectionConfig>>();
        
        _services.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        scopeFactory.CreateScope().Returns(scope);
        scope.ServiceProvider.Returns(_services);
        _services.GetService(typeof(IOptions<DapperDataProtectionConfig>)).Returns(_options);
        _services.GetService(typeof(IDbDataProtectionStorage)).Returns(_provider);
        _services.GetService(typeof(ILoggerFactory)).Returns(loggerFactory);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 0)]
    public void UseDapperDataProtection_InitializeTable_InitializesDbOnlyWhenEnabled(bool initializeTable, int expectedCalls)
    {
        _options.Value.Returns(new DapperDataProtectionConfig
        {
            SchemaName = "",
            TableName = "",
            InitializeTable = initializeTable
        });

        _services.UseDapperDataProtection();

        _provider.Received(expectedCalls).InitializeDb();
    }
}