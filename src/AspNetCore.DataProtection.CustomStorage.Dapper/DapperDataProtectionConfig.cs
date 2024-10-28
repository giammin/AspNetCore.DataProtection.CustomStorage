namespace AspNetCore.DataProtection.CustomStorage.Dapper;

/// <summary>
/// dapper storage implementation configuration 
/// </summary>
public record DapperDataProtectionConfig
{
    /// <summary>
    /// schema name
    /// </summary>
    public required string SchemaName { get; set; }
    /// <summary>
    /// table name
    /// </summary>
    public required string TableName { get; set; }

    /// <summary>
    /// if true at start it will create the table if not present
    /// Default=true
    /// </summary>
    public bool InitializeTable { get; set; }=true;
}