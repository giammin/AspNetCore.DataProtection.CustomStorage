using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests;

public class DataProtectionStorageExtensionsTests
{
    [Fact]
    public void PersistKeysToStorage_StorageNotDeletable_RepositoryDoesNotSupportDeletion()
    {
        using var provider = BuildProvider<InMemoryStorage>();

        provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository
            .Should().NotBeNull().And.NotBeAssignableTo<IDeletableXmlRepository>();
        provider.GetRequiredService<IKeyManager>().As<XmlKeyManager>().CanDeleteKeys
            .Should().BeFalse();
    }

    [Fact]
    public void PersistKeysToStorage_StorageDeletable_RepositorySupportsDeletion()
    {
        using var provider = BuildProvider<DeletableInMemoryStorage>();

        provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value.XmlRepository
            .Should().BeAssignableTo<IDeletableXmlRepository>();
        provider.GetRequiredService<IKeyManager>().As<XmlKeyManager>().CanDeleteKeys
            .Should().BeTrue();
    }

    [Fact]
    public void DeleteKeys_StorageDeletable_DeletesMatchingKeysFromStorage()
    {
        using var provider = BuildProvider<DeletableInMemoryStorage>();
        // XmlKeyManager exposes DeleteKeys but does not implement IDeletableKeyManager.
        var keyManager = (XmlKeyManager)provider.GetRequiredService<IKeyManager>();
        var now = DateTimeOffset.UtcNow;
        var kept = keyManager.CreateNewKey(now, now.AddDays(90));
        var deleted = keyManager.CreateNewKey(now, now.AddDays(90));

        var result = keyManager.DeleteKeys(key => key.KeyId == deleted.KeyId);

        result.Should().BeTrue();
        keyManager.GetAllKeys().Select(k => k.KeyId).Should().Equal(kept.KeyId);
        provider.GetRequiredService<DeletableInMemoryStorage>().Keys.Should().ContainSingle()
            .Which.Xml.Should().Contain(kept.KeyId.ToString());
    }

    private static ServiceProvider BuildProvider<TStorage>() where TStorage : class, IDataProtectionStorage
    {
        var services = new ServiceCollection();
        services.AddSingleton<TStorage>();
        services.AddDataProtection().PersistKeysToStorage<TStorage>();
        return services.BuildServiceProvider();
    }

    private class InMemoryStorage : IDataProtectionStorage
    {
        public List<DataProtectionKey> Keys { get; } = [];

        public IEnumerable<DataProtectionKey> GetAll() => Keys.ToList();

        public void Insert(DataProtectionKey key) => Keys.Add(key);
    }

    private sealed class DeletableInMemoryStorage : InMemoryStorage, IDeletableDataProtectionStorage
    {
        public void Delete(IReadOnlyCollection<DataProtectionKey> keys)
        {
            foreach (var key in keys)
            {
                Keys.Remove(key);
            }
        }
    }
}
