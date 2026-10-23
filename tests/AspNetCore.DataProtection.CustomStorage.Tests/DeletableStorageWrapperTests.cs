using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests;

public class DeletableStorageWrapperTests
{
    private const string KeyMaterial = TestConstants.KeyMaterial;

    private readonly IServiceProvider _services;
    private readonly IDeletableDataProtectionStorage _storage;
    private readonly CapturingLoggerFactory _loggerFactory = new();
    private readonly DeletableStorageWrapper<IDeletableDataProtectionStorage> _sut;

    private readonly DataProtectionKey _first = CreateKey("key-1");
    private readonly DataProtectionKey _second = CreateKey("key-2");
    private readonly DataProtectionKey _third = CreateKey("key-3");

    public DeletableStorageWrapperTests()
    {
        _services = Substitute.For<IServiceProvider>();
        _storage = Substitute.For<IDeletableDataProtectionStorage>();

        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_services);
        var serviceScopeFactory = Substitute.For<IServiceScopeFactory>();
        serviceScopeFactory.CreateScope().Returns(scope);

        _services.GetService(typeof(IServiceScopeFactory)).Returns(serviceScopeFactory);
        _services.GetService(typeof(IDeletableDataProtectionStorage)).Returns(_storage);
        _storage.GetAll().Returns([_first, _second, _third]);

        _sut = new DeletableStorageWrapper<IDeletableDataProtectionStorage>(_services, _loggerFactory);
    }

    [Fact]
    public void DeleteElements_ChooseElementsIsNull_ThrowsArgumentNullException()
    {
        _sut.Invoking(x => x.DeleteElements(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void DeleteElements_SnapshotContainsStoredElements()
    {
        IReadOnlyCollection<IDeletableElement>? snapshot = null;

        _sut.DeleteElements(elements => snapshot = elements);

        snapshot.Should().NotBeNull();
        snapshot!.Select(e => e.Element.ToString(SaveOptions.DisableFormatting))
            .Should().Equal(_first.Xml, _second.Xml, _third.Xml);
        snapshot.Should().OnlyContain(e => e.DeletionOrder == null);
    }

    [Fact]
    public void DeleteElements_RowWithMalformedXml_ExcludedFromSnapshot()
    {
        var malformed = new DataProtectionKey { FriendlyName = "key-malformed", Xml = "<key>" };
        _storage.GetAll().Returns([malformed, _first]);
        IReadOnlyCollection<IDeletableElement>? snapshot = null;

        _sut.DeleteElements(elements => snapshot = elements);

        snapshot.Should().ContainSingle()
            .Which.Element.ToString(SaveOptions.DisableFormatting).Should().Be(_first.Xml);
    }

    [Fact]
    public void DeleteElements_NoElementChosen_ReturnsTrueWithoutDeleting()
    {
        var result = _sut.DeleteElements(_ => { });

        result.Should().BeTrue();
        _storage.DidNotReceiveWithAnyArgs().Delete(null!);
    }

    [Fact]
    public void DeleteElements_ElementsChosen_DeletesStoredKeysInDeletionOrder()
    {
        var result = _sut.DeleteElements(elements =>
        {
            var list = elements.ToList();
            list[0].DeletionOrder = 2;
            list[2].DeletionOrder = 1;
        });

        result.Should().BeTrue();
        _storage.Received(1).Delete(Arg.Is<IReadOnlyCollection<DataProtectionKey>>(keys =>
            keys.Count == 2 && ReferenceEquals(keys.First(), _third) && ReferenceEquals(keys.Last(), _first)));
    }

    [Fact]
    public void DeleteElements_ElementsChosen_LogsDeletedKeysWithoutKeyXml()
    {
        _sut.DeleteElements(elements => elements.First().DeletionOrder = 1);

        _loggerFactory.Entries.Should().ContainSingle(e => e.EventId.Name == "DeletedKeyFromStorage")
            .Which.Contains(_first.FriendlyName!).Should().BeTrue();
        _loggerFactory.Entries.Should().NotContain(e => e.Contains(KeyMaterial));
    }

    [Fact]
    public void DeleteElements_StorageFails_ReturnsFalseAndLogsErrorWithoutKeyXml()
    {
        var storageException = new InvalidOperationException("delete failed");
        _storage.WhenForAnyArgs(x => x.Delete(null!)).Throw(storageException);

        var result = _sut.DeleteElements(elements => elements.First().DeletionOrder = 1);

        result.Should().BeFalse();
        _loggerFactory.Entries.Should().ContainSingle(e => e.Level == LogLevel.Error)
            .Which.Exception.Should().BeSameAs(storageException);
        _loggerFactory.Entries.Should().NotContain(e => e.EventId.Name == "DeletedKeyFromStorage");
        _loggerFactory.Entries.Should().NotContain(e => e.Contains(KeyMaterial));
    }

    private static DataProtectionKey CreateKey(string friendlyName) => new()
    {
        FriendlyName = friendlyName,
        Xml = new XElement("key", new XAttribute("id", friendlyName), new XElement("masterKey", KeyMaterial))
            .ToString(SaveOptions.DisableFormatting)
    };
}
