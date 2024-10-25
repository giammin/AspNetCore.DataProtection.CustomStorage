using System;
using System.Threading.Tasks;
using AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;
using FluentAssertions;
using Npgsql;
using Renci.SshNet.Security;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests.PostgreSQL;
public class PostgreSqlDataProtectionRepositoryTests: IClassFixture<PostgreSqlContainerFixture>
{
    private readonly PostgreSqlContainerFixture _fixture;
    private readonly PostgreSQLDataProtectionRepository _sut;


    public PostgreSqlDataProtectionRepositoryTests(PostgreSqlContainerFixture fixture)
    {
        _fixture = fixture;
        _sut = new PostgreSQLDataProtectionRepository(fixture.NpgsqlDataSource, fixture.Options);
    }

    [Fact]
    public void Insert_NullKey_ThrowsExeption()
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
            .Should().Throw<PostgresException>().WithMessage("*duplicate key value*");
    }

    [Theory]
    [InlineData("","xml1")]
    [InlineData("   ", "xml2")]
    [InlineData(null, "xml3")]
    public void Insert_FriendlyNameIsNullOrEmptyString_PersistsData(string? friendlyName, string xml)
    {
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
