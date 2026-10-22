using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetCore.DataProtection.CustomStorage;

/// <summary>
/// handle retrieving, storing and deleting the keys for the DataProtection with a storage
/// that implements <see cref="IDeletableDataProtectionStorage"/>
/// </summary>
/// <typeparam name="TStorage">the registered storage type; it must implement <see cref="IDeletableDataProtectionStorage"/></typeparam>
internal class DeletableStorageWrapper<TStorage> : StorageWrapper<TStorage>, IDeletableXmlRepository
    where TStorage : IDataProtectionStorage
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="services"></param>
    /// <param name="loggerFactory"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"><typeparamref name="TStorage"/> does not implement <see cref="IDeletableDataProtectionStorage"/></exception>
    public DeletableStorageWrapper(IServiceProvider services, ILoggerFactory loggerFactory)
        : base(services, loggerFactory)
    {
        if (!typeof(TStorage).IsAssignableTo(typeof(IDeletableDataProtectionStorage)))
        {
            throw new ArgumentException(
                $"'{typeof(TStorage).Name}' does not implement '{nameof(IDeletableDataProtectionStorage)}'.",
                nameof(TStorage));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// A stored key whose XML is malformed is logged and left out of the snapshot, so it is never deleted.
    /// The chosen keys are deleted with a single <see cref="IDeletableDataProtectionStorage.Delete"/> call;
    /// if it fails, the error is logged and <c>false</c> is returned.
    /// </remarks>
    public bool DeleteElements(Action<IReadOnlyCollection<IDeletableElement>> chooseElements)
    {
        ArgumentNullException.ThrowIfNull(chooseElements);
        using var scope = _services.CreateScope();
        var storage = (IDeletableDataProtectionStorage)scope.ServiceProvider.GetRequiredService<TStorage>();

        var deletableElements = new List<DeletableElement>();
        foreach (var key in storage.GetAll())
        {
            var element = TryParseKey(key);
            if (element is not null)
            {
                deletableElements.Add(new DeletableElement(key, element));
            }
        }

        chooseElements(deletableElements);

        var keysToDelete = deletableElements
            .Where(e => e.DeletionOrder.HasValue)
            .OrderBy(e => e.DeletionOrder.GetValueOrDefault())
            .Select(e => e.Key)
            .ToList();
        if (keysToDelete.Count == 0)
        {
            return true;
        }

        foreach (var key in keysToDelete)
        {
            _logger.LogDeletingKeyFromStorage(key.FriendlyName, typeof(TStorage).Name);
        }

        try
        {
            storage.Delete(keysToDelete);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogErrorDeletingKeysFromStorage(e, keysToDelete.Count, typeof(TStorage).Name);
            return false;
        }
    }

    private sealed class DeletableElement(DataProtectionKey key, XElement element) : IDeletableElement
    {
        public DataProtectionKey Key { get; } = key;

        public XElement Element { get; } = element;

        public int? DeletionOrder { get; set; }
    }
}
