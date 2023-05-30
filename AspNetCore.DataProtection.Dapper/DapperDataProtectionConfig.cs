namespace AspNetCore.DataProtection.Dapper;

/// <summary>
/// dapper storage implementation configuration 
/// </summary>
public record DapperDataProtectionConfig
{
    /// <summary>
    /// schema name
    /// </summary>
    public string SchemaName { get; set; } = "dbo";
    /// <summary>
    /// table name
    /// </summary>
    public string TableName { get; set; } = "DataProtectionKeys";

    /// <summary>
    /// if true at start it will create the table if not present
    /// Default=true
    /// </summary>
    public bool InitializeTable { get; set; }=true;
    /// <summary>
    /// use <see cref="DapperDataProtectionRepository"/> as repository
    /// if you want to use your implementation you need do register it as <see cref="IDataProtectionStorage"/>
    /// and set this property false
    /// Default=true
    /// </summary>
    public bool UseDefaultStorageImplementation { get; set; }=true;

}