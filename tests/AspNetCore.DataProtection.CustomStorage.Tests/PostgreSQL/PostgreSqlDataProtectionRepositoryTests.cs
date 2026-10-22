using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;
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
    public async Task Insert_FriendlyNameIsNullOrEmptyString_PersistsData(string? friendlyName, string xml)
    {
        await _fixture.ResetDatabaseAsync();
        var key = new DataProtectionKey
        {
            FriendlyName = friendlyName,
            Xml = xml
        };
        _sut.Insert(key);

        var stored = _sut.GetAll().Should().ContainSingle().Subject;
        stored.FriendlyName.Should().Be(friendlyName);
        stored.Xml.Should().Be(xml);
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
    public void InitializeDb_CustomSchemaAndTable_PersistsKeysInConfiguredTable()
    {
        const string schemaName = "custom_keys";
        var tableName = $"keys_{Guid.NewGuid().ToString("N")[..12]}";
        using (var connection = _fixture.NpgsqlDataSource.OpenConnection())
        {
            connection.Execute($"CREATE SCHEMA IF NOT EXISTS {schemaName};");
        }
        var options = Substitute.For<IOptionsSnapshot<DapperDataProtectionConfig>>();
        options.Value.Returns(new DapperDataProtectionConfig { SchemaName = schemaName, TableName = tableName });
        var sut = new PostgreSQLDataProtectionRepository(_fixture.NpgsqlDataSource, options);
        var key = new DataProtectionKey { FriendlyName = "CustomSchemaKey", Xml = "Xml" };

        sut.InitializeDb();
        sut.Insert(key);

        sut.GetAll().Should().ContainSingle(x => x.FriendlyName == key.FriendlyName && x.Xml == key.Xml);
        using (var connection = _fixture.NpgsqlDataSource.OpenConnection())
        {
            connection.ExecuteScalar<long>($"SELECT COUNT(*) FROM {schemaName}.{tableName};").Should().Be(1);
        }
        _sut.GetAll().Should().NotContain(x => x.FriendlyName == key.FriendlyName);
    }
}
