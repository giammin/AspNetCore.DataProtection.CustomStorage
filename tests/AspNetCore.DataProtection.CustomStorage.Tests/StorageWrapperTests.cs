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
     private readonly ILogger _logger = Substitute.For<ILogger>();
    private readonly IDataProtectionStorage _storage;

     public StorageWrapperTests()
     {
         _services = Substitute.For<IServiceProvider>();
         _loggerFactory = Substitute.For<ILoggerFactory>();
         _storage = Substitute.For<IDataProtectionStorage>();

         var scope = Substitute.For<IServiceScope>();
         scope.ServiceProvider.Returns(_services);

         var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
         serviceScopeFactory.CreateScope().Returns(scope);

         _services.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);
         _services.GetService(typeof(IDataProtectionStorage)).Returns(_storage);
         _services.GetService(typeof(IDbDataProtectionStorage)).Returns(_storage);
        
         _loggerFactory.CreateLogger(Arg.Any<string>()).Returns(_logger);
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
            FriendlyName = TestConstants.FriendlyName.Trim(),
            Xml = TestConstants.XElement.ToString(SaveOptions.DisableFormatting)
        };
        _storage.Received(1).Insert(Arg.Is(key));
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
            key != null && key.FriendlyName == "TestName" && key.Xml == element.ToString(SaveOptions.DisableFormatting)));
    }

    private const string KeyMaterial = "c2VjcmV0LWtleS1tYXRlcmlhbA==";
    private const string KeyFriendlyName = "key-6f1c2b9e";
    private static readonly XElement KeyElement = new("key", new XElement("masterKey", KeyMaterial));

    [Fact]
    public void GetAllElements_KeyRead_DoesNotLogKeyXml()
    {
        var loggerFactory = new CapturingLoggerFactory();
        var sut = new StorageWrapper<IDataProtectionStorage>(_services, loggerFactory);
        _storage.GetAll().Returns([new DataProtectionKey { FriendlyName = KeyFriendlyName, Xml = KeyElement.ToString() }]);

        sut.GetAllElements();

        loggerFactory.Entries.Should().Contain(e => e.Contains(KeyFriendlyName));
        loggerFactory.Entries.Should().NotContain(e => e.Contains(KeyMaterial));
    }

    [Fact]
    public void StoreElement_KeySaved_DoesNotLogKeyXml()
    {
        var loggerFactory = new CapturingLoggerFactory();
        var sut = new StorageWrapper<IDataProtectionStorage>(_services, loggerFactory);

        sut.StoreElement(KeyElement, KeyFriendlyName);

        loggerFactory.Entries.Should().Contain(e => e.Contains(KeyFriendlyName));
        loggerFactory.Entries.Should().NotContain(e => e.Contains(KeyMaterial));
    }

    [Fact]
    public void StoreElement_StorageFails_LogsErrorWithStorageExceptionAndWithoutKeyXml()
    {
        var loggerFactory = new CapturingLoggerFactory();
        var sut = new StorageWrapper<IDataProtectionStorage>(_services, loggerFactory);
        var storageException = new InvalidOperationException("insert failed");
        _storage.WhenForAnyArgs(x => x.Insert(null!)).Throw(storageException);

        sut.Invoking(x => x.StoreElement(KeyElement, KeyFriendlyName)).Should().Throw<KeyInsertException>();

        loggerFactory.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
            .Which.Exception.Should().BeSameAs(storageException);
        loggerFactory.Entries.Should().NotContain(e => e.Contains(KeyMaterial));
    }

    [Fact]
    public void StoreElement_StorageFails_ExceptionMessageNamesKeyWithoutKeyXml()
    {
        var storageException = new InvalidOperationException("insert failed");
        _storage.WhenForAnyArgs(x => x.Insert(null!)).Throw(storageException);

        var exception = _sut.Invoking(x => x.StoreElement(KeyElement, KeyFriendlyName))
            .Should().Throw<KeyInsertException>().Which;

        exception.Message.Should().Contain(KeyFriendlyName).And.NotContain(KeyMaterial);
        exception.InnerException.Should().BeSameAs(storageException);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\r\n")]
    public void StoreElement_FriendlyNameNullOrWhiteSpace_InsertsNullFriendlyName(string? friendlyName)
    {
        _sut.StoreElement(KeyElement, friendlyName);

        _storage.Received(1).Insert(Arg.Is<DataProtectionKey>(key => key.FriendlyName == null));
    }

    [Fact]
    public void StoreElement_FriendlyNameWithSurroundingWhiteSpace_InsertsTrimmedFriendlyName()
    {
        _sut.StoreElement(KeyElement, $"  {KeyFriendlyName}  ");

        _storage.Received(1).Insert(Arg.Is<DataProtectionKey>(key => key.FriendlyName == KeyFriendlyName));
    }

    private const string MalformedFriendlyName = "key-malformed";
    private static readonly string MalformedKeyXml = $"<key><masterKey>{KeyMaterial}</masterKey>";

    [Fact]
    public void GetAllElements_RowWithMalformedXml_SkipsRowAndReturnsValidKeys()
    {
        _storage.GetAll().Returns([
            new DataProtectionKey { FriendlyName = MalformedFriendlyName, Xml = MalformedKeyXml },
            new DataProtectionKey { FriendlyName = KeyFriendlyName, Xml = KeyElement.ToString() }
        ]);

        var elements = _sut.GetAllElements();

        elements.Should().ContainSingle()
            .Which.ToString().Should().Be(KeyElement.ToString());
    }

    [Fact]
    public void GetAllElements_RowWithMalformedXml_LogsErrorNamingRowWithoutKeyXml()
    {
        var loggerFactory = new CapturingLoggerFactory();
        var sut = new StorageWrapper<IDataProtectionStorage>(_services, loggerFactory);
        _storage.GetAll().Returns([new DataProtectionKey { FriendlyName = MalformedFriendlyName, Xml = MalformedKeyXml }]);

        sut.GetAllElements().Should().BeEmpty();

        loggerFactory.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
            .Which.Should().Match<LogEntry>(e => e.EventId.Name == "SkippingMalformedKey" && e.Contains(MalformedFriendlyName));
        loggerFactory.Entries.Should().NotContain(e => e.Contains(KeyMaterial));
    }
}