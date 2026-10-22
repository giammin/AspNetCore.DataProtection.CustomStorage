namespace AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;

/// <summary>
/// constants
/// </summary>
public static class Constants
{
    /// <summary>
    /// default schema name
    /// </summary>
    public const string DefaultSchema = "public";
    /// <summary>
    /// default keys table name
    /// </summary>
    public const string TableName = "data_protection_keys";
    /// <summary>
    /// service key of the <see cref="Npgsql.NpgsqlDataSource"/> registered by the provider,
    /// so that it does not collide with a data source registered by the application
    /// </summary>
    internal const string DataSourceServiceKey = "AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL";
}