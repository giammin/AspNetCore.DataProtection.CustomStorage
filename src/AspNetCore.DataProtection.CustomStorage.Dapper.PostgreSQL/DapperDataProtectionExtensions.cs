using System;
using Dapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;

/// <summary>
/// Extension method class
/// </summary>
public static class DapperDataProtectionExtensions
{
    /// <summary>
    /// Configures the data protection system to persist keys to a sql server storage
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/> instance to modify.</param>
    /// <param name="connectionString"></param>
    /// <param name="configAction"></param>
    /// <returns>The value <paramref name="builder"/>.</returns>
    public static IDataProtectionBuilder PersistKeysWithDapperInSqlServer(this IDataProtectionBuilder builder,
        string connectionString,
        Action<DapperDataProtectionConfig>? configAction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var config = new DapperDataProtectionConfig
        {
            SchemaName = "public",
            TableName = "data_protection_keys"
        };
        configAction?.Invoke(config);
        builder.Services.Configure<DapperDataProtectionConfig>(c =>
        {
            c.InitializeTable = config.InitializeTable;
            c.SchemaName = config.SchemaName;
            c.TableName = config.TableName;
        });
        
        var npgsqlDataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
#if DEBUG
        npgsqlDataSourceBuilder.ConnectionStringBuilder.IncludeErrorDetail = true;
#endif
        var npgsqlDataSource = npgsqlDataSourceBuilder.Build();

        //dapper
        DefaultTypeMap.MatchNamesWithUnderscores = true;

        builder.Services.AddSingleton(npgsqlDataSource);

        builder.Services.AddScoped<IDbDataProtectionStorage, PostgreSQLDataProtectionRepository>();
        builder.PersistKeysToStorage<IDbDataProtectionStorage>();
        return builder;
    }
}