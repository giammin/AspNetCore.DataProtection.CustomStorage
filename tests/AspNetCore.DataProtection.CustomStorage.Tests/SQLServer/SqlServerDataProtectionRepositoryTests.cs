using System;
using System.Linq;
using System.Threading.Tasks;
using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;
using Dapper;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;
using Constants = AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer.Constants;

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
    public void InitializeDb_TableExistsWithoutIndex_CreatesIndex()
    {
        var tableName = NewTableName();
        var sut = CreateSut(tableName);
        sut.InitializeDb();
        DropFriendlyNameIndex(tableName);

        sut.InitializeDb();

        FriendlyNameIndexExists(tableName).Should().BeTrue();
        var key = new DataProtectionKey { FriendlyName = "FriendlyName", Xml = "Xml" };
        sut.Insert(key);
        sut.Invoking(x => x.Insert(key))
            .Should().Throw<SqlException>().WithMessage("*duplicate key*");
    }

    [Fact]
    public void InitializeDb_TableAndIndexExist_Succeeds()
    {
        var tableName = NewTableName();
        var sut = CreateSut(tableName);
        sut.InitializeDb();

        sut.Invoking(x => x.InitializeDb()).Should().NotThrow();

        FriendlyNameIndexExists(tableName).Should().BeTrue();
    }

    [Fact]
    public void InitializeDb_TableWithoutIndexHasDuplicateFriendlyNames_ThrowsClearError()
    {
        var tableName = NewTableName();
        var sut = CreateSut(tableName);
        sut.InitializeDb();
        DropFriendlyNameIndex(tableName);
        var key = new DataProtectionKey { FriendlyName = "FriendlyName", Xml = "Xml" };
        sut.Insert(key);
        sut.Insert(key);

        sut.Invoking(x => x.InitializeDb())
            .Should().Throw<SqlException>()
            .WithMessage($"*IX_{tableName}_FriendlyName*duplicate non-null*FriendlyName*");

        FriendlyNameIndexExists(tableName).Should().BeFalse();
    }

    [Fact]
    public void InitializeDb_TableWithoutIndexHasDuplicateNullFriendlyNames_CreatesIndex()
    {
        var tableName = NewTableName();
        var sut = CreateSut(tableName);
        sut.InitializeDb();
        DropFriendlyNameIndex(tableName);
        sut.Insert(new DataProtectionKey { FriendlyName = null, Xml = "xml1" });
        sut.Insert(new DataProtectionKey { FriendlyName = null, Xml = "xml2" });

        sut.InitializeDb();

        FriendlyNameIndexExists(tableName).Should().BeTrue();
    }

    private static string NewTableName() => $"Keys_{Guid.NewGuid():N}";

    private SQLServerDataProtectionRepository CreateSut(string tableName)
    {
        var options = Substitute.For<IOptionsSnapshot<DapperDataProtectionConfig>>();
        options.Value.Returns(new DapperDataProtectionConfig
        {
            SchemaName = Constants.DefaultSchema,
            TableName = tableName
        });
        return new SQLServerDataProtectionRepository(_fixture.SqlConnectionFactory, options);
    }

    private void DropFriendlyNameIndex(string tableName)
    {
        using var connection = _fixture.SqlConnectionFactory.CreateConnection();
        connection.Execute(
            $"DROP INDEX [IX_{tableName}_FriendlyName] ON [{Constants.DefaultSchema}].[{tableName}];");
    }

    private bool FriendlyNameIndexExists(string tableName)
    {
        using var connection = _fixture.SqlConnectionFactory.CreateConnection();
        return connection.ExecuteScalar<int>(
            """
            SELECT COUNT(*)
            FROM [sys].[indexes]
            WHERE [object_id] = OBJECT_ID(@TableName)
              AND [name] = @IndexName
              AND [is_unique] = 1
              AND [has_filter] = 1;
            """,
            new
            {
                TableName = $"[{Constants.DefaultSchema}].[{tableName}]",
                IndexName = $"IX_{tableName}_FriendlyName"
            }) == 1;
    }
}
