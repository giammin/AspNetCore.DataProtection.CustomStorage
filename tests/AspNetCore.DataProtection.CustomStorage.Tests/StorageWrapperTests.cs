using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests;

public class StorageWrapperTests
{
    private readonly StorageWrapper<IDataProtectionStorage> _sut;
    private readonly IServiceProvider _services;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IServiceScope _scope;
    private readonly CapturingLoggerFactory _loggerFactory = new();
    private readonly IDataProtectionStorage _storage;

    public StorageWrapperTests()
    {
        _services = Substitute.For<IServiceProvider>();
        _storage = Substitute.For<IDataProtectionStorage>();

        // The storage is resolvable only from the scoped provider, not from the root one.
        var scopedServices = Substitute.For<IServiceProvider>();
        scopedServices.GetService(typeof(IDataProtectionStorage)).Returns(_storage);

        _scope = Substitute.For<IServiceScope>();
        _scope.ServiceProvider.Returns(scopedServices);

        _scopeFactory = Substitute.For<IServiceScopeFactory>();
        _scopeFactory.CreateScope().Returns(_scope);

        _services.GetService(typeof(IServiceScopeFactory)).Returns(_scopeFactory);

        _sut = new StorageWrapper<IDataProtectionStorage>(_services, _loggerFactory);
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
            FriendlyName = TestConstants.FriendlyName,
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

    private const string KeyMaterial = TestConstants.KeyMaterial;
    private const string KeyFriendlyName = "key-6f1c2b9e";
    private static readonly XElement KeyElement = new("key", new XElement("masterKey", KeyMaterial));

    [Fact]
    public void GetAllElements_KeyRead_DoesNotLogKeyXml()
    {
        _storage.GetAll().Returns([new DataProtectionKey { FriendlyName = KeyFriendlyName, Xml = KeyElement.ToString() }]);

        _sut.GetAllElements();

        _loggerFactory.Entries.Should().Contain(e => e.Contains(KeyFriendlyName));
        _loggerFactory.Entries.Should().NotContain(e => e.Contains(KeyMaterial));
    }

    [Fact]
    public void StoreElement_KeySaved_DoesNotLogKeyXml()
    {
        _sut.StoreElement(KeyElement, KeyFriendlyName);

        _loggerFactory.Entries.Should().Contain(e => e.Contains(KeyFriendlyName));
        _loggerFactory.Entries.Should().NotContain(e => e.Contains(KeyMaterial));
    }

    [Fact]
    public void StoreElement_StorageFails_LogsErrorWithStorageExceptionAndWithoutKeyXml()
    {
        var storageException = new InvalidOperationException("insert failed");
        _storage.WhenForAnyArgs(x => x.Insert(null!)).Throw(storageException);

        _sut.Invoking(x => x.StoreElement(KeyElement, KeyFriendlyName)).Should().Throw<KeyInsertException>();

        _loggerFactory.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
            .Which.Exception.Should().BeSameAs(storageException);
        _loggerFactory.Entries.Should().NotContain(e => e.Contains(KeyMaterial));
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
        _storage.GetAll().Returns([new DataProtectionKey { FriendlyName = MalformedFriendlyName, Xml = MalformedKeyXml }]);

        _sut.GetAllElements().Should().BeEmpty();

        _loggerFactory.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
            .Which.Should().Match<LogEntry>(e => e.EventId.Name == "SkippingMalformedKey" && e.Contains(MalformedFriendlyName));
        _loggerFactory.Entries.Should().NotContain(e => e.Contains(KeyMaterial));
    }

    [Fact]
    public void GetAllElements_EmptyStorage_ReturnsEmpty()
    {
        _storage.GetAll().Returns([]);

        _sut.GetAllElements().Should().BeEmpty();
    }

    [Fact]
    public void GetAllElements_StoredKeys_ReturnsParsedElementsInStorageOrder()
    {
        var first = new XElement("key", new XAttribute("id", "1"), new XElement("masterKey", "first"));
        var second = new XElement("key", new XAttribute("id", "2"), new XElement("masterKey", "second"));
        _storage.GetAll().Returns([
            new DataProtectionKey { FriendlyName = "first", Xml = first.ToString(SaveOptions.DisableFormatting) },
            new DataProtectionKey { FriendlyName = null, Xml = second.ToString() }
        ]);

        var elements = _sut.GetAllElements();

        elements.Select(e => e.ToString()).Should().Equal(first.ToString(), second.ToString());
    }

    [Fact]
    public void GetAllElements_EachCall_ResolvesStorageFromNewScopeAndDisposesIt()
    {
        _storage.GetAll().Returns([]);

        _sut.GetAllElements();
        _sut.GetAllElements();

        _scopeFactory.Received(2).CreateScope();
        _scope.Received(2).Dispose();
        _storage.Received(2).GetAll();
    }
}