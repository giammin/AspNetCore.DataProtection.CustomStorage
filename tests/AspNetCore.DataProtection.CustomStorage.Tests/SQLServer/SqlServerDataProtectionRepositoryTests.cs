using System;
using System.Threading.Tasks;
using AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Npgsql;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests.SQLServer;
public class SqlServerDataProtectionRepositoryTests : IClassFixture<SqlServerContainerFixture>
{
    private readonly SqlServerContainerFixture _fixture;
    private readonly SQLServerDataProtectionRepository _sut;
    
    public SqlServerDataProtectionRepositoryTests(SqlServerContainerFixture fixture)
    {
        _fixture = fixture;
        _sut = new SQLServerDataProtectionRepository(fixture.SqlConnectionFactory, fixture.Options);
    }

    [Fact]
    public void Insert_NullKey_ThrowsException()
    {
        _sut.Invoking(x => x.Insert(null!))
            .Should().Throw<ArgumentNullException>();
    }
    [Fact]
    public void Insert_XmlAndFriendlyNameNotNull_PersistsData()
    {
        var key = new DataProtectionKey
        {
            FriendlyName = "FriendlyName",
            Xml = "Xml"
        };
        _sut.Insert(key);
        _sut.GetAll()
            .Should()
            .ContainSingle(x=>x.Xml==key.Xml && x.FriendlyName==key.FriendlyName);
    }
    [Fact]
    public void Insert_FriendlyNameDuplicated_ThrowException()
    {
        var key = new DataProtectionKey
        {
            FriendlyName = "FriendlyNameDuplicated",
            Xml = "Xml"
        };
        _sut.Insert(key);
        _sut.Invoking(x => x.Insert(key))
            .Should().Throw<SqlException>().WithMessage("*duplicate key*");
    }

    [Theory]
    [InlineData("","xml1")]
    [InlineData("   ", "xml2")]
    [InlineData(null, "xml3")]
    public async Task Insert_FriendlyNameIsNullOrEmptyString_PersistsData(string? friendlyName, string xml)
    {
        await _fixture.ResetDatabaseAsync();
        var key = new DataProtectionKey
        {
            FriendlyName = friendlyName,
            Xml = xml
        };
        _sut.Insert(key);
    }
    [Fact]
    public async Task GetAllAsync_NoKeys_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();
        _sut.GetAll().Should().BeEmpty();
    }
}
