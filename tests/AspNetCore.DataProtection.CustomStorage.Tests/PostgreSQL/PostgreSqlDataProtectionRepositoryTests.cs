using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;
using Dapper;
using FluentAssertions;
using Npgsql;
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
    public async Task GetAll_NoKeys_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();
        _sut.GetAll().Should().BeEmpty();
    }
    [Fact]
    public async Task GetAllAsync_NoKeys_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();
        (await _sut.GetAllAsync()).Should().BeEmpty();
    }
    [Fact]
    public async Task GetAllAsync_KeysInserted_ReturnsEntitiesWithIdAndInsertDate()
    {
        await _fixture.ResetDatabaseAsync();
        var named = new DataProtectionKey { FriendlyName = "GetAllAsyncNamed", Xml = "xml1" };
        var unnamed = new DataProtectionKey { FriendlyName = null, Xml = "xml2" };
        _sut.Insert(named);
        _sut.Insert(unnamed);

        var entities = (await _sut.GetAllAsync()).ToList();

        entities.Should().HaveCount(2)
            .And.Contain(x => x.FriendlyName == named.FriendlyName && x.Xml == named.Xml)
            .And.Contain(x => x.FriendlyName == null && x.Xml == unnamed.Xml)
            .And.OnlyContain(x => x.Id > 0 && x.InsertDate != default);
        entities.Select(x => x.Id).Should().OnlyHaveUniqueItems();
    }
    [Fact]
    public async Task GetAllAsync_MatchNamesWithUnderscoresDisabled_MapsAllColumns()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = false;
        await _fixture.ResetDatabaseAsync();
        var key = new DataProtectionKey { FriendlyName = "MappingFriendlyName", Xml = "MappingXml" };
        _sut.Insert(key);

        var entity = (await _sut.GetAllAsync()).Should().ContainSingle().Subject;

        entity.Id.Should().BePositive();
        entity.InsertDate.Should().NotBe(default);
        entity.FriendlyName.Should().Be(key.FriendlyName);
        entity.Xml.Should().Be(key.Xml);
    }
}
