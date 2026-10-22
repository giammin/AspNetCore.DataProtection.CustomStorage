using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AspNetCore.DataProtection.CustomStorage;

/// <summary>
/// Extension method class for configuring instances of <see cref="IDataProtectionStorage"/>
/// </summary>
public static class DataProtectionStorageExtensions
{
    /// <summary>
    /// Configures the data protection system to persist keys to a custom storage.
    /// When <typeparamref name="TStorage"/> implements <see cref="IDeletableDataProtectionStorage"/>,
    /// keys can also be deleted through <see cref="XmlKeyManager.DeleteKeys"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IDataProtectionBuilder"/> instance to modify.</param>
    /// <returns>The value <paramref name="builder"/>.</returns>
    public static IDataProtectionBuilder PersistKeysToStorage<TStorage>(this IDataProtectionBuilder builder)
        where TStorage : IDataProtectionStorage
    {
        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var loggerFactory = services.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlRepository = typeof(TStorage).IsAssignableTo(typeof(IDeletableDataProtectionStorage))
                    ? new DeletableStorageWrapper<TStorage>(services, loggerFactory)
                    : new StorageWrapper<TStorage>(services, loggerFactory);
            });
        });

        return builder;
    }
}