namespace AspNetCore.DataProtection.CustomStorage;

/// <summary>
/// Storage/repository interface
/// </summary>
public interface IDataProtectionStorage
{
    /// <summary>
    /// return all stored keys
    /// </summary>
    /// <returns></returns>
    IEnumerable<DataProtectionKey> GetAll();

    /// <summary>
    /// insert a new <see cref="DataProtectionKey"/>
    /// <see cref="DataProtectionKey.FriendlyName"/> must be unique when not null
    /// </summary>
    /// <param name="key"></param>
    void Insert(DataProtectionKey key);
}