using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AspNetCore.DataProtection.CustomStorage.Dapper;

/// <summary>
/// Extension method class for configuring <see cref="IDataProtectionStorage"/>
/// </summary>
public static class DapperDataProtectionExtensions
{
    /// <summary>
    /// Initialize db and check service registrations
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static IServiceProvider UseDapperDataProtection(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IOptions<DapperDataProtectionConfig>>().Value;
        var provider = scope.ServiceProvider.GetRequiredService<IDbDataProtectionStorage>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DapperDataProtectionExtensions));
        logger.LogInitialization(config.ToString(), provider.GetType().ToString());
        if (config.InitializeTable)
        {
            provider.InitializeDb();
        }

        return services;
    }
}
