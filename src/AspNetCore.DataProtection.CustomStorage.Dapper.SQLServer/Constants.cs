namespace AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;

/// <summary>
/// constants
/// </summary>
public static class Constants
{
    /// <summary>
    /// default schema name
    /// </summary>
    public const string DefaultSchema = "dbo";
    /// <summary>
    /// default keys table name
    /// </summary>
    public const string TableName = "DataProtectionKeys";
    /// <summary>
    /// service key of the <see cref="SqlConnectionFactory"/> registered by the provider,
    /// so that it does not collide with a connection factory registered by the application
    /// </summary>
    internal const string ConnectionFactoryServiceKey = "AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer";
}