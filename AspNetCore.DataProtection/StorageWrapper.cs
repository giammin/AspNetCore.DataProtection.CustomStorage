using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetCore.DataProtection;

/// <summary>
/// handle retrieving and store the keys for the DataProtection with a generic storage
/// </summary>
/// <typeparam name="TStorage"></typeparam>
internal class StorageWrapper<TStorage> : IXmlRepository where TStorage : IDataProtectionStorage
{
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="loggerFactory"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public StorageWrapper(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _logger = loggerFactory.CreateLogger<StorageWrapper<TStorage>>();
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <inheritdoc />
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = _services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<TStorage>();
        return storage.GetAll().Select(key =>
        {
            _logger.ReadingXmlFromKey(key.FriendlyName, key.Xml);
            return XElement.Parse(key.Xml);
        }).ToImmutableArray();
    }

    /// <inheritdoc />
    public void StoreElement(XElement element, string? friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (!string.IsNullOrWhiteSpace(friendlyName))
        {
            //todo friendlyName if not null must be unique
        }
        else
        {
            friendlyName = null;
        }
        using var scope = _services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<TStorage>();

        var key = new DataProtectionKey
        {
            FriendlyName = friendlyName,
            Xml = element.ToString(SaveOptions.DisableFormatting)
        };
        _logger.LogSavingKeyToStorage(key.FriendlyName, key.Xml, typeof(TStorage).Name);
        try
        {
            storage.Insert(key);
        }
        catch (Exception e)
        {
            _logger.LogErrorSavingKeyToStorage(key.FriendlyName, key.Xml, typeof(TStorage).Name);

            throw new KeyInsertException($"Cannot insert key {key}", e);
        }
    }
}