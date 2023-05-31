namespace AspNetCore.DataProtection.CustomStorage.Dapper;

/// <inheritdoc />
public interface IDbDataProtectionStorage:IDataProtectionStorage
{
    /// <summary>
    /// create the storage/table if needed
    /// </summary>
    void InitializeDb();
}