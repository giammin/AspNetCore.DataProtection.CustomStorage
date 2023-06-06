using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.Tests;

public class DapperDataProtectionExtensionsTests
{
    [Fact]
    public void PersistKeysWithDapper_UseDefaultStorageImplementationIsTrue_UsesDapperImplementation()
    {
        //var serviceCollection = new ServiceCollection();
        //serviceCollection
        //    .AddDataProtection()
        //    .PersistKeysWithDapper();
        //var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
        //var keyManagementOptions = serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>();

        //Assert.IsAssignableFrom<IDbDataProtectionStorage>(keyManagementOptions.Value.XmlRepository);
        Assert.Fail("not implemented");
    }
    [Fact]
    public void PersistKeysWithDapper_UseDefaultStorageImplementationIsFalse_UsesCustomImplementation()
    {
        //var serviceCollection = new ServiceCollection();
        //serviceCollection
        //    .AddDataProtection()
        //    .PersistKeysWithDapper();
        //var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
        //var keyManagementOptions = serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>();

        //Assert.IsAssignableFrom<IDbDataProtectionStorage>(keyManagementOptions.Value.XmlRepository);
        Assert.Fail("not implemented");
    }
    [Fact]
    public void UseDapperDataProtection_ProviderNotRegistered_ThrowException()
    {
        //var serviceCollection = new ServiceCollection();
        //serviceCollection
        //    .AddDataProtection()
        //    .PersistKeysWithDapper();
        //var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
        //var keyManagementOptions = serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>();

        //Assert.IsAssignableFrom<IDbDataProtectionStorage>(keyManagementOptions.Value.XmlRepository);
        Assert.Fail("not implemented");
    }
    [Fact]
    public void UseDapperDataProtection_UseDefaultStorageImplementationIsTrueAndIDbConnectionNotRegistered_ThrowException()
    {
        //var serviceCollection = new ServiceCollection();
        //serviceCollection
        //    .AddDataProtection()
        //    .PersistKeysWithDapper();
        //var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
        //var keyManagementOptions = serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>();

        //Assert.IsAssignableFrom<IDbDataProtectionStorage>(keyManagementOptions.Value.XmlRepository);
        Assert.Fail("not implemented");
    }
    [Fact]
    public void UseDapperDataProtection_InitializeTable_ExecInitializeDb()
    {
        //var serviceCollection = new ServiceCollection();
        //serviceCollection
        //    .AddDataProtection()
        //    .PersistKeysWithDapper();
        //var serviceProvider = serviceCollection.BuildServiceProvider(validateScopes: true);
        //var keyManagementOptions = serviceProvider.GetRequiredService<IOptions<KeyManagementOptions>>();

        //Assert.IsAssignableFrom<IDbDataProtectionStorage>(keyManagementOptions.Value.XmlRepository);
        Assert.Fail("not implemented");
    }
}