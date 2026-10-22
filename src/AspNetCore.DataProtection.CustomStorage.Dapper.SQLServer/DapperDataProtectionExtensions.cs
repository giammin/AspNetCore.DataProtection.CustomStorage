using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;

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
            SchemaName = Constants.DefaultSchema,
            TableName = Constants.TableName
        };
        configAction?.Invoke(config);
        builder.Services.AddOptions<DapperDataProtectionConfig>()
            .Configure(c =>
            {
                c.InitializeTable = config.InitializeTable;
                c.SchemaName = config.SchemaName;
                c.TableName = config.TableName;
            })
            .ValidateOnStart();
        builder.Services.TryAddEnumerable(ServiceDescriptor
            .Singleton<IValidateOptions<DapperDataProtectionConfig>, DapperDataProtectionConfigValidator>());

        builder.Services.AddSingleton(_=>new SqlConnectionFactory(connectionString));
        builder.Services.AddScoped<IDbDataProtectionStorage, SQLServerDataProtectionRepository>();
        builder.PersistKeysToStorage<IDbDataProtectionStorage>();
        return builder;
    }
}