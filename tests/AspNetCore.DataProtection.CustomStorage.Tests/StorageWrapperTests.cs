using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System;
using Xunit;
namespace AspNetCore.DataProtection.CustomStorage.Tests;

//internal class FakeDataProtectionStorage:IDataProtectionStorage
//{
//    public IEnumerable<DataProtectionKey> GetAll()
//    {
//        throw new NotImplementedException();
//    }

//    public void Insert(DataProtectionKey key)
//    {
//        throw new NotImplementedException();
//    }
//}

public class StorageWrapperTests
{
    private readonly ILogger<StorageWrapper<IDataProtectionStorage>> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly StorageWrapper<IDataProtectionStorage> _storageWrapper;

    public StorageWrapperTests()
    {
        _logger = Substitute.For<ILogger<StorageWrapper<IDataProtectionStorage>>>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger<StorageWrapper<IDataProtectionStorage>>().Returns(_logger);
        _serviceProvider = Substitute.For<IServiceProvider>();
        _storageWrapper = new StorageWrapper<IDataProtectionStorage>(_serviceProvider, loggerFactory);
    }
    
    [Fact]
    public void StoreElement_ValidKey_CallsStorageSave()
    {
        //// Arrange
        //var key = new DataProtectionKey()
        //{
        //    FriendlyName = "FriendlyName",
        //    Xml = "xml"
        //};
        //var storage = Substitute.For<IDataProtectionStorage>();
        //_serviceProvider.GetService<IDataProtectionStorage>().Returns(storage);

        //// Act
        //_storageWrapper.StoreElement("test-key", "data");

        //// Assert
        //storage.Received(1).Insert("test-key", "data");
    }
    [Fact]
    public void StorageWrapperCtor_ContextIsNull_ThrowException()
    {
        //var fake = Substitute.For<IDataProtectionStorage>();
        Assert.Throws<ArgumentNullException>(() => new StorageWrapper<IDataProtectionStorage>(null!, null!));
    }

    [Fact]
    public void StoreElement_XmlElementAndFriendlyNameNotNull_PersistsData()
    {
        Assert.Fail("integration");

    }
    [Fact]
    public void StoreElement_FriendlyNameDuplicated_ThrowException()
    {
        Assert.Fail("integration");

    }

    [Fact]
        public void StoreElement_FriendlyNameIsNullOrEmptyString_PersistsData()
    {
        Assert.Fail("integration");

        //var element = XElement.Parse("<Element1/>");
        //var friendlyName = "Element1";
        //var key = new DataProtectionKey() { FriendlyName = friendlyName, Xml = element.ToString() };

        //var services = GetServices(nameof(StoreElement_PersistsData));
        //var service = new EntityFrameworkCoreXmlRepository<DataProtectionKeyContext>(services, NullLoggerFactory.Instance);
        //service.StoreElement(element, friendlyName);

        //// Use a separate instance of the context to verify correct data was saved to database
        //using (var context = services.CreateScope().ServiceProvider.GetRequiredService<DataProtectionKeyContext>())
        //{
        //    Assert.Equal(1, context.DataProtectionKeys.Count());
        //    Assert.Equal(key.FriendlyName, context.DataProtectionKeys.Single()?.FriendlyName);
        //    Assert.Equal(key.Xml, context.DataProtectionKeys.Single()?.Xml);
        //}
    }

    [Fact]
    public void StoreElement_XElementIsNull_ThrowsException()
    {
        Assert.Fail("not implemented");

    }
    [Fact]
    public void StoreElement_StorageFails_ThrowsKeyInsertException()
    {
        Assert.Fail("not implemented");

    }


}
