using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using FluentAssertions;
using Xunit;
using AspNetCore.DataProtection.CustomStorage.Dapper;

namespace AspNetCore.DataProtection.CustomStorage.Tests;

public class StorageWrapperTests
{
    private readonly ILogger<StorageWrapper<IDataProtectionStorage>> _logger = Substitute.For<ILogger<StorageWrapper<IDataProtectionStorage>>>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();
    private readonly IDataProtectionStorage _storage = Substitute.For<IDataProtectionStorage>();
    private readonly StorageWrapper<IDataProtectionStorage> _sut;
    public StorageWrapperTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger<StorageWrapper<IDataProtectionStorage>>().Returns(_logger);

        var provider = Substitute.For<IDbDataProtectionStorage>();
        var scope = Substitute.For<IServiceScope>();
        var scopeFactory = Substitute.For<IServiceScopeFactory>();



        _serviceProvider.GetService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        _serviceProvider.GetRequiredService(typeof(IServiceScopeFactory)).Returns(scopeFactory);
        _serviceProvider.CreateScope().Returns(scope);
        _serviceProvider.GetService(typeof(IDataProtectionStorage)).Returns(provider);



        _sut = new StorageWrapper<IDataProtectionStorage>(_serviceProvider, loggerFactory);

    }
    
    [Fact]
    public void StoreElement_ValidKey_CallsStorageSave()
    {
        _sut.StoreElement(TestConstants.XElement, TestConstants.FriendlyName);

        _storage.Received(1).Insert(new DataProtectionKey(){FriendlyName = TestConstants.FriendlyName, Xml = TestConstants.XElement.ToString()});
    }
    [Fact]
    public void StorageWrapperCtor_ContextIsNull_ThrowException()
    {
        Assert.Throws<ArgumentNullException>(() => new StorageWrapper<IDataProtectionStorage>(null!, null!));
    }


    [Fact]
    public void StoreElement_XElementIsNull_ThrowsException()
    {
        _sut.Invoking(x => x.StoreElement(null!, TestConstants.FriendlyName))
            .Should().Throw<ArgumentNullException>();
    }
    [Fact]
    public void StoreElement_StorageFails_ThrowsKeyInsertException()
    {
        _storage.WhenForAnyArgs(x => x.Insert(null!)).Throw<Exception>();

        _sut.Invoking(x => x.StoreElement(TestConstants.XElement,TestConstants.FriendlyName))
            .Should().Throw<KeyInsertException>();
    }


}