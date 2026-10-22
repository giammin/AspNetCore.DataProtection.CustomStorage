using System;
using System.Linq;
using System.Threading;
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
public class SqlServerDataProtectionRepositoryTests
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
    public void Delete_NullKeys_ThrowsArgumentNullException()
    {
        _sut.Invoking(x => x.Delete(null!))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task Delete_KeysReturnedByGetAll_DeletesOnlyThoseKeys()
    {
        await _fixture.ResetDatabaseAsync();
        _sut.Insert(new DataProtectionKey { FriendlyName = "DeleteKept", Xml = "xml1" });
        _sut.Insert(new DataProtectionKey { FriendlyName = "DeleteRemoved", Xml = "xml2" });
        _sut.Insert(new DataProtectionKey { FriendlyName = null, Xml = "xml3" });
        var keysToDelete = _sut.GetAll().Where(x => x.FriendlyName != "DeleteKept").ToList();

        _sut.Delete(keysToDelete);

        _sut.GetAll().Should().ContainSingle()
            .Which.FriendlyName.Should().Be("DeleteKept");
    }

    [Fact]
    public async Task Delete_KeyNotReturnedByGetAll_ThrowsArgumentExceptionAndDeletesNothing()
    {
        await _fixture.ResetDatabaseAsync();
        _sut.Insert(new DataProtectionKey { FriendlyName = "DeleteKept", Xml = "xml1" });
        var stored = _sut.GetAll().Single();

        _sut.Invoking(x => x.Delete([stored, new DataProtectionKey { FriendlyName = "DeleteKept", Xml = "xml1" }]))
            .Should().Throw<ArgumentException>();

        _sut.GetAll().Should().ContainSingle();
    }

    [Fact]
    public async Task Delete_KeyAlreadyDeleted_Succeeds()
    {
        await _fixture.ResetDatabaseAsync();
        _sut.Insert(new DataProtectionKey { FriendlyName = "DeleteTwice", Xml = "xml1" });
        var stored = _sut.GetAll().ToList();
        _sut.Delete(stored);

        _sut.Invoking(x => x.Delete(stored)).Should().NotThrow();
    }

    [Fact]
    public void Delete_LaterDeletionFails_RollsBackEarlierDeletions()
    {
        var tableName = NewTableName();
        var sut = CreateSut(tableName);
        sut.InitializeDb();
        sut.Insert(new DataProtectionKey { FriendlyName = "DeleteFirst", Xml = "xml1" });
        sut.Insert(new DataProtectionKey { FriendlyName = "DeleteFails", Xml = "xml2" });
        using (var connection = _fixture.SqlConnectionFactory.CreateConnection())
        {
            connection.Execute(
                $"""
                 CREATE TRIGGER [{Constants.DefaultSchema}].[TR_{tableName}_FailDelete]
                 ON [{Constants.DefaultSchema}].[{tableName}] AFTER DELETE AS
                 IF EXISTS(SELECT 1 FROM deleted WHERE [FriendlyName] = N'DeleteFails')
                     THROW 50000, N'delete failed', 1;
                 """);
        }
        var keys = sut.GetAll().OrderBy(x => x.FriendlyName == "DeleteFails").ToList();

        sut.Invoking(x => x.Delete(keys)).Should().Throw<SqlException>().WithMessage("*delete failed*");

        sut.GetAll().Select(x => x.FriendlyName).Should().BeEquivalentTo("DeleteFirst", "DeleteFails");
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

    [Fact]
    public void InitializeDb_NewTable_InsertDateIsDatetime2WithUtcDefault()
    {
        var tableName = NewTableName();
        var sut = CreateSut(tableName);

        sut.InitializeDb();

        using var connection = _fixture.SqlConnectionFactory.CreateConnection();
        var column = connection.QuerySingle<(string TypeName, string DefaultName, string DefaultDefinition)>(
            """
            SELECT t.[name], dc.[name], dc.[definition]
            FROM [sys].[columns] c
              JOIN [sys].[types] t ON t.[user_type_id] = c.[user_type_id]
              JOIN [sys].[default_constraints] dc ON dc.[object_id] = c.[default_object_id]
            WHERE c.[object_id] = OBJECT_ID(@TableName)
              AND c.[name] = N'InsertDate';
            """,
            new { TableName = $"[{Constants.DefaultSchema}].[{tableName}]" });
        column.Should().Be(("datetime2", $"DF_{tableName}_InsertDate", "(sysutcdatetime())"));
    }

    [Fact]
    public void InitializeDb_CustomSchemaAndTable_PersistsKeysInConfiguredTable()
    {
        const string schemaName = "custom_keys";
        var tableName = NewTableName();
        using (var connection = _fixture.SqlConnectionFactory.CreateConnection())
        {
            connection.Execute($"IF SCHEMA_ID(N'{schemaName}') IS NULL EXEC(N'CREATE SCHEMA [{schemaName}]');");
        }
        var sut = CreateSut(tableName, schemaName);
        var key = new DataProtectionKey { FriendlyName = "CustomSchemaKey", Xml = "Xml" };

        sut.InitializeDb();
        sut.Insert(key);

        sut.GetAll().Should().ContainSingle(x => x.FriendlyName == key.FriendlyName && x.Xml == key.Xml);
        CountRows(schemaName, tableName).Should().Be(1);
        _sut.GetAll().Should().NotContain(x => x.FriendlyName == key.FriendlyName);
    }

    [Fact]
    public async Task InitializeDb_ConcurrentCallsOnNewTable_AllSucceed()
    {
        const int callCount = 10;
        // Pooled connections make the calls overlap; with cold connections the race rarely shows.
        var connections = Enumerable.Range(0, callCount).Select(_ => _fixture.SqlConnectionFactory.CreateConnection()).ToList();
        connections.ForEach(connection => connection.Open());
        connections.ForEach(connection => connection.Dispose());

        // Several rounds, because a single round does not always hit the race.
        for (var round = 0; round < 5; round++)
        {
            var tableName = NewTableName();
            // One repository per call, as on separate nodes starting at the same time.
            var suts = Enumerable.Range(0, callCount).Select(_ => CreateSut(tableName)).ToList();
            using var barrier = new Barrier(callCount);

            var calls = suts.Select(sut => Task.Factory.StartNew(() =>
            {
                barrier.SignalAndWait();
                sut.InitializeDb();
            }, TaskCreationOptions.LongRunning)).ToList();

            await FluentActions.Awaiting(() => Task.WhenAll(calls)).Should().NotThrowAsync();
            FriendlyNameIndexExists(tableName).Should().BeTrue();
        }
    }

    [Fact]
    public void InitializeDb_SchemaMissing_CreatesSchemaAndTable()
    {
        var schemaName = $"Schema_{Guid.NewGuid():N}";
        var tableName = NewTableName();
        var sut = CreateSut(tableName, schemaName);

        sut.InitializeDb();
        sut.Insert(new DataProtectionKey { FriendlyName = "MissingSchemaKey", Xml = "Xml" });

        CountRows(schemaName, tableName).Should().Be(1);
        sut.Invoking(x => x.InitializeDb()).Should().NotThrow();
    }

    private static string NewTableName() => $"Keys_{Guid.NewGuid():N}";

    private SQLServerDataProtectionRepository CreateSut(string tableName, string schemaName = Constants.DefaultSchema)
    {
        var options = Substitute.For<IOptionsSnapshot<DapperDataProtectionConfig>>();
        options.Value.Returns(new DapperDataProtectionConfig
        {
            SchemaName = schemaName,
            TableName = tableName
        });
        return new SQLServerDataProtectionRepository(_fixture.SqlConnectionFactory, options);
    }

    private int CountRows(string schemaName, string tableName)
    {
        using var connection = _fixture.SqlConnectionFactory.CreateConnection();
        return connection.ExecuteScalar<int>($"SELECT COUNT(*) FROM [{schemaName}].[{tableName}];");
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
