using System;
using System.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;

/// <summary>
/// Extension method class
/// </summary>
public static class DapperDataProtectionExtensions
{
    
    /// <summary>
    /// Configures the data protection system to persist keys to a PostgreSQL storage
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/> instance to modify.</param>
    /// <param name="connectionString"></param>
    /// <param name="configAction"></param>
    /// <returns>The value <paramref name="builder"/>.</returns>
    /// <exception cref="InvalidOperationException">an <see cref="IDbDataProtectionStorage"/> is already registered</exception>
    public static IDataProtectionBuilder PersistKeysWithDapperInPostgreSQL(this IDataProtectionBuilder builder,
        string connectionString,
        Action<DapperDataProtectionConfig>? configAction = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        // A second registration would silently replace the first one and share its options.
        if (builder.Services.Any(d => d.ServiceType == typeof(IDbDataProtectionStorage)))
        {
            throw new InvalidOperationException(
                $"An {nameof(IDbDataProtectionStorage)} is already registered: call {nameof(PersistKeysWithDapperInPostgreSQL)} " +
                "or PersistKeysWithDapperInSqlServer only once.");
        }

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
        
        // Keyed, so that a data source registered by the application is neither replaced nor used;
        // created by a factory, so that the container owns and disposes it.
        builder.Services.AddKeyedSingleton(Constants.DataSourceServiceKey,
            (_, _) => new NpgsqlDataSourceBuilder(connectionString).Build());

        builder.Services.AddScoped<IDbDataProtectionStorage, PostgreSQLDataProtectionRepository>();
        builder.PersistKeysToStorage<IDbDataProtectionStorage>();
        return builder;
    }
}