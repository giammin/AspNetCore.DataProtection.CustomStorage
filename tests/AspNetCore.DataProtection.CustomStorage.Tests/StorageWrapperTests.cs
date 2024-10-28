using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;
using AspNetCore.DataProtection.CustomStorage.Dapper;

namespace AspNetCore.DataProtection.CustomStorage.Tests;

public class StorageWrapperTests
{
     private readonly StorageWrapper<IDataProtectionStorage> _sut;
     private readonly IServiceProvider _services;
     private readonly ILoggerFactory _loggerFactory;
     private readonly ILogger<StorageWrapper<IDataProtectionStorage>> _logger;
     private readonly IDataProtectionStorage _storage;

     public StorageWrapperTests()
     {
         _services = Substitute.For<IServiceProvider>();
         _loggerFactory = Substitute.For<ILoggerFactory>();
         _logger = Substitute.For<ILogger<StorageWrapper<IDataProtectionStorage>>>();
         _storage = Substitute.For<IDataProtectionStorage>();

         var scope = Substitute.For<IServiceScope>();
         scope.ServiceProvider.Returns(_services);

         var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
         serviceScopeFactory.CreateScope().Returns(scope);

         // Configure IServiceProvider to return IServiceScopeFactory
         _services.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);
         _services.GetService(typeof(IDataProtectionStorage)).Returns(_storage);
         _services.GetService(typeof(IDbDataProtectionStorage)).Returns(_storage);
        
         _loggerFactory.CreateLogger<StorageWrapper<IDataProtectionStorage>>().Returns(_logger);
         _sut = new StorageWrapper<IDataProtectionStorage>(_services, _loggerFactory);
     }
     [Fact]
     public void Constructo_ContextIsNull_ThrowException()
     {
         Assert.Throws<ArgumentNullException>(() => new StorageWrapper<IDataProtectionStorage>(null!, null!));
     }
     [Fact]
     public void Constructor_LoggerFactoryIsNull_ThrowArgumentNullException()
     {
         // Act
         Action act = () =>
         {
             var _ = new StorageWrapper<IDataProtectionStorage>(_services, null!);
         };

         // Assert
         act.Should().Throw<ArgumentNullException>().WithMessage("*loggerFactory*");
     }

     [Fact]
     public void Constructor_ServiceProviderIsNull_ThrowArgumentNullException()
     {
         // Act
         var act = () =>
         {
             var _ = new StorageWrapper<IDataProtectionStorage>(null!, _loggerFactory);
         };

         // Assert
         act.Should().Throw<ArgumentNullException>().WithMessage("*services*");
     }
    [Fact]
    public void StoreElement_ValidKey_CallsStorageSave()
    {
        _sut.StoreElement(TestConstants.XElement, TestConstants.FriendlyName);
        var key = new DataProtectionKey
        {
            FriendlyName = TestConstants.FriendlyName?.Trim(),
            Xml = TestConstants.XElement.ToString(SaveOptions.DisableFormatting)
        };
        _storage.Received(1).Insert( Arg.Is(key));
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

    

    [Fact]
    public void StoreElement_ShouldCallInsertOnStorage()
    {
        // Arrange
        var element = new XElement("TestElement");

        // Act
        _sut.StoreElement(element, "TestName");

        // Assert
        _storage.Received(1).Insert(Arg.Is<DataProtectionKey>(key =>
            key.FriendlyName == "TestName" && key.Xml == element.ToString(SaveOptions.DisableFormatting)));
    }

}