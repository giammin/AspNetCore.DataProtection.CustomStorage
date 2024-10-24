using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspNetCore.DataProtection.CustomStorage.Dapper;

/// <inheritdoc />
public interface IDbDataProtectionStorage:IDataProtectionStorage
{
    /// <summary>
    /// create the storage/table if needed
    /// </summary>
    void InitializeDb();
    /// <summary>
    /// return all the <see cref="DataProtectionKeyEntity"/> saved
    /// </summary>
    /// <returns></returns>
    public Task<IEnumerable<DataProtectionKeyEntity>> GetAllAsync();
}