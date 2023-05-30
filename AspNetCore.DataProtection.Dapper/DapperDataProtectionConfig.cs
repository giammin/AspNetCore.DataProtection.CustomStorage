namespace AspNetCore.DataProtection.Dapper;

/// <summary>
/// dapper storage implementation configuration 
/// </summary>
public record DapperDataProtectionConfig
{
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = "DataProtectionKeys";

    public bool InitializeTable { get; set; }=true;
    public bool UseDefaultStorageImplementation { get; set; }=true;

}