using System.Data;

namespace AspNetCore.DataProtection.Dapper;

public class DataProtectionRepository:IDataProtectionStorage
{
    private readonly IDbConnection _connection;

    public DataProtectionRepository(IDbConnection connection)
    {
        _connection = connection;
    }
    public IEnumerable<DataProtectionKey> GetAll()
    {
        throw new NotImplementedException();
    }

    public void Insert(DataProtectionKey key)
    {
        throw new NotImplementedException();
    }
}