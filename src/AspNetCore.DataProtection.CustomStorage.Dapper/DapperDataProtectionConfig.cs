namespace AspNetCore.DataProtection.CustomStorage.Dapper;

/// <summary>
/// dapper storage implementation configuration 
/// </summary>
public record DapperDataProtectionConfig
{
    /// <summary>
    /// schema name
    /// </summary>
    /// <remarks>
    /// Validated at startup by the database provider: letters, digits and underscores, not starting with a digit
    /// (lowercase only for PostgreSQL), within the database identifier length limit.
    /// </remarks>
    public required string SchemaName { get; set; }
    /// <summary>
    /// table name
    /// </summary>
    /// <remarks>
    /// Validated at startup with the same rules as <see cref="SchemaName"/>.
    /// </remarks>
    public required string TableName { get; set; }

    /// <summary>
    /// if true at start it will create the schema, the table and its unique index if not present
    /// Default=true
    /// </summary>
    public bool InitializeTable { get; set; } = true;
}