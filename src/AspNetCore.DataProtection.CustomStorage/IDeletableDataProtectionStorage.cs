using System.Collections.Generic;

namespace AspNetCore.DataProtection.CustomStorage;

/// <summary>
/// Storage/repository interface that also supports the deletion of stored keys.
/// When the storage passed to <see cref="DataProtectionStorageExtensions.PersistKeysToStorage{TStorage}"/>
/// implements this interface, keys can be deleted through
/// <see cref="Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager.DeleteKeys"/>.
/// </summary>
public interface IDeletableDataProtectionStorage : IDataProtectionStorage
{
    /// <summary>
    /// delete the given keys, in the given order.
    /// The keys are instances previously returned by <see cref="IDataProtectionStorage.GetAll"/> on the same instance.
    /// Either every key is deleted or none is: on failure the implementation must throw and leave the storage unchanged.
    /// </summary>
    /// <param name="keys">the keys to delete, in deletion order</param>
    void Delete(IReadOnlyCollection<DataProtectionKey> keys);
}
