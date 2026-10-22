using System;
using System.Linq;
using System.Threading;
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
[Collection(nameof(PostgreSqlCollection))]
public class PostgreSqlDataProtectionRepositoryTests
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

    [Fact]
    public void InitializeDb_CustomSchemaAndTable_PersistsKeysInConfiguredTable()
    {
        const string schemaName = "custom_keys";
        var tableName = PostgreSqlContainerFixture.NewTableName();
        using (var connection = _fixture.NpgsqlDataSource.OpenConnection())
        {
            connection.Execute($"CREATE SCHEMA IF NOT EXISTS {schemaName};");
        }
        var sut = CreateSut(schemaName, tableName);
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
        var connections = Enumerable.Range(0, callCount).Select(_ => _fixture.NpgsqlDataSource.OpenConnection()).ToList();
        connections.ForEach(connection => connection.Dispose());

        // Several rounds, because a single round does not always hit the race.
        for (var round = 0; round < 5; round++)
        {
            var tableName = PostgreSqlContainerFixture.NewTableName();
            // One repository per call, as on separate nodes starting at the same time.
            var suts = Enumerable.Range(0, callCount)
                .Select(_ => CreateSut(Constants.DefaultSchema, tableName))
                .ToList();
            using var barrier = new Barrier(callCount);

            var calls = suts.Select(sut => Task.Factory.StartNew(() =>
            {
                barrier.SignalAndWait();
                sut.InitializeDb();
            }, TaskCreationOptions.LongRunning)).ToList();

            await FluentActions.Awaiting(() => Task.WhenAll(calls)).Should().NotThrowAsync();
            using var connection = _fixture.NpgsqlDataSource.OpenConnection();
            connection.ExecuteScalar<long>(
                    "SELECT COUNT(*) FROM pg_indexes WHERE schemaname = @Schema AND indexname = @Index;",
                    new { Schema = Constants.DefaultSchema, Index = $"ix_{Constants.DefaultSchema}_{tableName}_friendly_name" })
                .Should().Be(1);
        }
    }

    [Fact]
    public void InitializeDb_SchemaMissing_CreatesSchemaAndTable()
    {
        var schemaName = $"schema_{Guid.NewGuid().ToString("N")[..12]}";
        var tableName = PostgreSqlContainerFixture.NewTableName();
        var sut = CreateSut(schemaName, tableName);

        sut.InitializeDb();
        sut.Insert(new DataProtectionKey { FriendlyName = "MissingSchemaKey", Xml = "Xml" });

        CountRows(schemaName, tableName).Should().Be(1);
        sut.Invoking(x => x.InitializeDb()).Should().NotThrow();
    }

    [Fact]
    public void InitializeDb_ReservedWordSchemaName_PersistsKeys()
    {
        // A reserved word is accepted unquoted after a schema qualifier, but not as the schema name itself.
        const string schemaName = "order";
        var tableName = PostgreSqlContainerFixture.NewTableName();
        var sut = CreateSut(schemaName, tableName);
        var key = new DataProtectionKey { FriendlyName = "ReservedWordKey", Xml = "Xml" };

        sut.InitializeDb();
        sut.Insert(key);

        var stored = sut.GetAll().Should().ContainSingle().Subject;
        stored.FriendlyName.Should().Be(key.FriendlyName);
        sut.Delete([stored]);
        CountRows(schemaName, tableName).Should().Be(0);
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
        var tableName = PostgreSqlContainerFixture.NewTableName();
        var sut = CreateSut(Constants.DefaultSchema, tableName);
        sut.InitializeDb();
        sut.Insert(new DataProtectionKey { FriendlyName = "DeleteFirst", Xml = "xml1" });
        sut.Insert(new DataProtectionKey { FriendlyName = "DeleteFails", Xml = "xml2" });
        using (var connection = _fixture.NpgsqlDataSource.OpenConnection())
        {
            connection.Execute(
                $"""
                 CREATE FUNCTION {Constants.DefaultSchema}.fail_delete_{tableName}() RETURNS trigger AS $$
                 BEGIN
                     IF OLD.friendly_name = 'DeleteFails' THEN RAISE EXCEPTION 'delete failed'; END IF;
                     RETURN OLD;
                 END $$ LANGUAGE plpgsql;
                 CREATE TRIGGER tr_{tableName}_fail_delete BEFORE DELETE ON {Constants.DefaultSchema}.{tableName}
                 FOR EACH ROW EXECUTE FUNCTION {Constants.DefaultSchema}.fail_delete_{tableName}();
                 """);
        }
        var keys = sut.GetAll().OrderBy(x => x.FriendlyName == "DeleteFails").ToList();

        sut.Invoking(x => x.Delete(keys)).Should().Throw<PostgresException>().WithMessage("*delete failed*");

        sut.GetAll().Select(x => x.FriendlyName).Should().BeEquivalentTo("DeleteFirst", "DeleteFails");
    }

    private PostgreSQLDataProtectionRepository CreateSut(string schemaName, string tableName)
    {
        var options = Substitute.For<IOptionsSnapshot<DapperDataProtectionConfig>>();
        options.Value.Returns(new DapperDataProtectionConfig { SchemaName = schemaName, TableName = tableName });
        return new PostgreSQLDataProtectionRepository(_fixture.NpgsqlDataSource, options);
    }

    private long CountRows(string schemaName, string tableName)
    {
        using var connection = _fixture.NpgsqlDataSource.OpenConnection();
        return connection.ExecuteScalar<long>($"""SELECT COUNT(*) FROM "{schemaName}"."{tableName}";""");
    }
}
