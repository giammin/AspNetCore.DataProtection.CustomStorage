namespace AspNetCore.DataProtection;

public interface IDataProtectionStorage
{
    IEnumerable<DataProtectionKey> GetAll();
    void Insert(DataProtectionKey key);
}