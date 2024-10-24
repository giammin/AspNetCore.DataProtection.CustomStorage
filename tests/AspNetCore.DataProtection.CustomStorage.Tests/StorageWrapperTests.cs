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
    [Fact]
    public void StorageWrapperCtor_ContextIsNull_ThrowException()
    {
        //var fake = Substitute.For<IDataProtectionStorage>();
        Assert.Throws<ArgumentNullException>(() => new StorageWrapper<IDataProtectionStorage>(null!, null!));
    }

    [Fact]
    public void StoreElement_XmlElementAndFriendlyNameNotNull_PersistsData()
    {
        Assert.Fail("not implemented");

    }

    [Fact]
        public void StoreElement_FriendlyNameIsNullOrEmptyString_PersistsData()
    {
        Assert.Fail("not implemented");

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
    public void StoreElement_XmlIsNull_ThrowsException()
    {
        Assert.Fail("not implemented");

    }
    [Fact]
    public void StoreElement_StorageFailToSave_ThrowsKeyInsertException()
    {
        Assert.Fail("not implemented");

    }

    [Fact]
    public void GetAllElements_ReturnsAllElements()
    {
        Assert.Fail("not implemented");

        //var element1 = XElement.Parse("<Element1/>");
        //var element2 = XElement.Parse("<Element2/>");

        //var services = GetServices(nameof(GetAllElements_ReturnsAllElements));
        //var service1 = CreateRepo(services);
        //service1.StoreElement(element1, "element1");
        //service1.StoreElement(element2, "element2");

        //// Use a separate instance of the context to verify correct data was saved to database
        //var service2 = CreateRepo(services);
        //var elements = service2.GetAllElements();
        //Assert.Equal(2, elements.Count);
    }

}
