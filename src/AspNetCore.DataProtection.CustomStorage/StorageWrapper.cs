using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetCore.DataProtection.CustomStorage;

/// <summary>
/// handle retrieving and store the keys for the DataProtection with a generic storage
/// </summary>
/// <typeparam name="TStorage"></typeparam>
internal class StorageWrapper<TStorage> : IXmlRepository where TStorage : IDataProtectionStorage
{
    protected readonly IServiceProvider _services;
    protected readonly ILogger _logger;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="loggerFactory"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public StorageWrapper(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(services);

        _logger = loggerFactory.CreateLogger<StorageWrapper<TStorage>>();
        _services = services;
    }

    /// <inheritdoc />
    /// <remarks>
    /// A stored key whose XML is malformed is logged and skipped, so that a single corrupted row
    /// does not make the whole key ring unusable.
    /// </remarks>
    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = _services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<TStorage>();
        return ParseKeys(storage).Select(k => k.Element).ToImmutableArray();
    }

    /// <summary>
    /// read every stored key and parse its XML
    /// </summary>
    /// <param name="storage">the storage to read</param>
    /// <returns>each stored key with its parsed element; keys with malformed XML are logged and skipped</returns>
    protected IEnumerable<(DataProtectionKey Key, XElement Element)> ParseKeys(IDataProtectionStorage storage)
    {
        foreach (var key in storage.GetAll())
        {
            var element = TryParseKey(key);
            if (element is not null)
            {
                yield return (key, element);
            }
        }
    }

    private XElement? TryParseKey(DataProtectionKey key)
    {
        _logger.ReadingXmlFromKey(key.FriendlyName);
        try
        {
            return XElement.Parse(key.Xml);
        }
        catch (XmlException e)
        {
            _logger.LogSkippingMalformedKey(key.FriendlyName, e.LineNumber, e.LinePosition);
            return null;
        }
    }

    /// <inheritdoc />
    public void StoreElement(XElement element, string? friendlyName)
    {
        ArgumentNullException.ThrowIfNull(element);
        using var scope = _services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<TStorage>();

        var key = new DataProtectionKey
        {
            // A blank name is stored as null: the unique index on the friendly name allows any number of nulls.
            FriendlyName = string.IsNullOrWhiteSpace(friendlyName) ? null : friendlyName.Trim(),
            Xml = element.ToString(SaveOptions.DisableFormatting)
        };
        _logger.LogSavingKeyToStorage(key.FriendlyName, typeof(TStorage).Name);
        try
        {
            storage.Insert(key);
        }
        catch (Exception e)
        {
            _logger.LogErrorSavingKeyToStorage(e, key.FriendlyName, typeof(TStorage).Name);

            // The key XML is deliberately excluded: it may contain unencrypted key material.
            throw new KeyInsertException($"Cannot insert key '{key.FriendlyName}'", e);
        }
    }
}