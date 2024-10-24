using System;
using System.Data;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AspNetCore.DataProtection.CustomStorage.Dapper;

/// <summary>
/// Extension method class for configuring <see cref="DapperDataProtectionRepository"/> as <see cref="IDataProtectionStorage"/>
/// </summary>
public static class DapperDataProtectionExtensions
{
    /// <summary>
    /// Configures the data protection system to persist keys to a custom storage
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/> instance to modify.</param>
    /// <param name="configAction"></param>
    /// <returns>The value <paramref name="builder"/>.</returns>
    public static IDataProtectionBuilder PersistKeysWithDapper(this IDataProtectionBuilder builder, Action<DapperDataProtectionConfig>? configAction=null)
    {
        var config = new DapperDataProtectionConfig();
        configAction?.Invoke(config);

        builder.Services.Configure<DapperDataProtectionConfig>(c =>
        {
            c.InitializeTable = config.InitializeTable;
            c.SchemaName = config.SchemaName;
            c.TableName = config.TableName;
            c.UseDefaultStorageImplementation = config.UseDefaultStorageImplementation;
        });

        if (config.UseDefaultStorageImplementation)
        {
            builder.Services.AddScoped<IDbDataProtectionStorage, DapperDataProtectionRepository>();
        }
        builder.PersistKeysToStorage<IDbDataProtectionStorage>();

        return builder;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static IServiceProvider UseDapperDataProtection(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IOptions<DapperDataProtectionConfig>>().Value;
        var provider = scope.ServiceProvider.GetService<IDbDataProtectionStorage>()
                       ?? throw new Exception($"{nameof(IDbDataProtectionStorage)} missing from registered services");

        if (config.InitializeTable)
        {
            provider.InitializeDb();
        }

        if (config.UseDefaultStorageImplementation)
        {
            var _ = scope.ServiceProvider.GetService<IDbConnection>()
                    ?? throw new Exception($"{nameof(IDbConnection)} is required and it is not registered");
        }

        return services;
    }
}