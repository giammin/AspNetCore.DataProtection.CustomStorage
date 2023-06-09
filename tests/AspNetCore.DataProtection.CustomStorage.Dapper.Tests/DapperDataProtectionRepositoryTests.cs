using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.Tests;

public class DapperDataProtectionRepositoryTests
{
    [Fact]
    public void DapperDataProtectionRepositoryCtor_ContextIsNull_ThrowException()
    {
        Assert.Throws<ArgumentNullException>(() => new DapperDataProtectionRepository(null!, null!));
    }

    [Fact]
    public void Insert_DataProtectionKeyValid_PersistsData()
    {
        Assert.Fail("not implemented");
    }
    [Fact]
    public void Insert_FriendlyNameIsNullOrEmptyString_PersistsData()
    {
        Assert.Fail("not implemented");

    }
    [Fact]
    public void Insert_XmlIsNul_PersistsData()
    {
        Assert.Fail("not implemented");

    }

    [Fact]
    public void GetAll_ReturnsAllElements()
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
        //Assert.Equal(2, elements.Count);        Assert.Fail("not implemented");

    }
    [Fact]
    public void GetAll_GetAllInterface_ReturnTheSame()
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

    [Fact]
    public void InitializeDb_TableNotPresentAndCustomConfig_CreateTableRespectingConfig()
    {
        Assert.Fail("not implemented");

    }
    [Fact]
    public void InitializeDb_TableNotPresent_CreateTable()
    {
        Assert.Fail("not implemented");

    }
    [Fact]
    public void InitializeDb_TablePresent_CreateTable()
    {
        Assert.Fail("not implemented");

    }
}