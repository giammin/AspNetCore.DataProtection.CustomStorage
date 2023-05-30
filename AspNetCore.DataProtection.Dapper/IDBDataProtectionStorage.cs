namespace AspNetCore.DataProtection.Dapper;

/// <summary>
public interface IDbDataProtectionStorage:IDataProtectionStorage
{
    void InitializeDb();
}