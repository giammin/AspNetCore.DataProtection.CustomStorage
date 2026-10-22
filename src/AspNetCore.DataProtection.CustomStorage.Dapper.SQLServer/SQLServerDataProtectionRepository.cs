using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;

/// <inheritdoc />
public class SQLServerDataProtectionRepository : IDbDataProtectionStorage
{
    // Database-wide: initializations of different tables are serialized too, which is harmless.
    private const string InitializeDbLockResource = "AspNetCore.DataProtection.CustomStorage.InitializeDb";
    private readonly SqlConnectionFactory _connectionFactory;
    private readonly DapperDataProtectionConfig _config;

    private string GetAllQuery() => 
            $"""
                SELECT
                    [{nameof(DataProtectionKeyEntity.Id)}],
                    [{nameof(DataProtectionKeyEntity.InsertDate)}],
                    [{nameof(DataProtectionKeyEntity.FriendlyName)}],
                    [{nameof(DataProtectionKeyEntity.Xml)}]
                FROM [{_config.SchemaName}].[{_config.TableName}];
                """;
    /// <summary>
    /// create an instance of <see cref="SQLServerDataProtectionRepository"/>
    /// </summary>
    /// <param name="connectionFactory"></param>
    /// <param name="options">repository configuration</param>
    public SQLServerDataProtectionRepository(SqlConnectionFactory connectionFactory,
        IOptionsSnapshot<DapperDataProtectionConfig> options)
    {
        _connectionFactory = connectionFactory;
        _config = options.Value;
    }

    /// <inheritdoc />
    public IEnumerable<DataProtectionKey> GetAll() 
    {
        using var connection = _connectionFactory.CreateConnection();
        return connection.Query<DataProtectionKeyEntity>(GetAllQuery());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DataProtectionKeyEntity>> GetAllAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DataProtectionKeyEntity>(GetAllQuery());
    }

    /// <inheritdoc />
    public void Insert(DataProtectionKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        using var connection = _connectionFactory.CreateConnection();
        connection.Execute(
            $"""
             INSERT INTO [{_config.SchemaName}].[{_config.TableName}](
                    {nameof(DataProtectionKeyEntity.FriendlyName)}, 
                    {nameof(DataProtectionKeyEntity.Xml)}) 
             VALUES (
                    @{nameof(DataProtectionKeyEntity.FriendlyName)},
                    @{nameof(DataProtectionKeyEntity.Xml)})
             """, key);
    }

    /// <inheritdoc />
    /// <remarks>
    /// The rows are deleted by <see cref="DataProtectionKeyEntity.Id"/>, in the given order, in a single transaction.
    /// A row that no longer exists is not an error.
    /// </remarks>
    /// <exception cref="ArgumentException">a key is not a <see cref="DataProtectionKeyEntity"/> returned by <see cref="GetAll"/></exception>
    public void Delete(IReadOnlyCollection<DataProtectionKey> keys)
    {
        var ids = GetIds(keys);
        if (ids.Count == 0)
        {
            return;
        }

        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        // Dapper executes the statement once per id, in order.
        connection.Execute(
            $"""
             DELETE FROM [{_config.SchemaName}].[{_config.TableName}]
             WHERE [{nameof(DataProtectionKeyEntity.Id)}] = @{nameof(DataProtectionKeyEntity.Id)}
             """,
            ids.Select(id => new { Id = id }),
            transaction);
        transaction.Commit();
    }

    private static List<int> GetIds(IReadOnlyCollection<DataProtectionKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        return keys
            .Select(key => key as DataProtectionKeyEntity ?? throw new ArgumentException(
                $"Every key must be a {nameof(DataProtectionKeyEntity)} returned by {nameof(GetAll)}.", nameof(keys)))
            .Select(entity => entity.Id)
            .ToList();
    }

    /// <inheritdoc />
    /// <remarks>
    /// Safe to call concurrently from several instances: the initialization is serialized
    /// with an exclusive application lock held until the transaction ends.
    /// </remarks>
    public void InitializeDb()
    {
        var indexName = $"IX_{_config.TableName}_{nameof(DataProtectionKeyEntity.FriendlyName)}";
        using var connection = _connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        // Without the lock, concurrent existence checks can all pass and the later CREATE statements fail.
        // The unique index is ensured independently of the table, so that an existing table
        // without the index (created manually, or with the index dropped) gets it restored.
        connection.Execute(
            $"""
               DECLARE @lockResult int;
               EXEC @lockResult = sp_getapplock @Resource = @LockResource, @LockMode = N'Exclusive', @LockOwner = N'Transaction';
               IF @lockResult < 0
                  THROW 50000, N'Cannot acquire the application lock to initialize the data protection keys table.', 1;
               IF NOT EXISTS(SELECT t.[object_id]
               FROM [sys].[tables] t
                  JOIN sys.schemas s ON s.schema_id=t.schema_id
               WHERE t.[name] = N'{_config.TableName}' AND s.name=N'{_config.SchemaName}')
               BEGIN
                   CREATE TABLE [{_config.SchemaName}].[{_config.TableName}](
                       [{nameof(DataProtectionKeyEntity.Id)}] [int] IDENTITY(1,1) NOT NULL,
                       [{nameof(DataProtectionKeyEntity.InsertDate)}] [datetime2] NOT NULL CONSTRAINT [DF_{_config.TableName}_{nameof(DataProtectionKeyEntity.InsertDate)}] DEFAULT sysutcdatetime(),
                      [{nameof(DataProtectionKeyEntity.FriendlyName)}] [nvarchar](256) NULL,
                      [{nameof(DataProtectionKeyEntity.Xml)}] [nvarchar](max) NOT NULL,
                      CONSTRAINT [PK_{_config.TableName}] PRIMARY KEY CLUSTERED ([{nameof(DataProtectionKeyEntity.Id)}] ASC) ON [PRIMARY]
                  ) ON [PRIMARY];
               END;
               IF NOT EXISTS(SELECT i.[index_id]
               FROM [sys].[indexes] i
               WHERE i.[object_id] = OBJECT_ID(N'[{_config.SchemaName}].[{_config.TableName}]') AND i.[name] = N'{indexName}')
               BEGIN
                  IF EXISTS(SELECT [{nameof(DataProtectionKeyEntity.FriendlyName)}]
                  FROM [{_config.SchemaName}].[{_config.TableName}]
                  WHERE [{nameof(DataProtectionKeyEntity.FriendlyName)}] IS NOT NULL
                  GROUP BY [{nameof(DataProtectionKeyEntity.FriendlyName)}]
                  HAVING COUNT(*) > 1)
                     THROW 50000, N'Cannot create unique index [{indexName}] on [{_config.SchemaName}].[{_config.TableName}]: the table contains duplicate non-null [{nameof(DataProtectionKeyEntity.FriendlyName)}] values. Remove the duplicates and initialize again.', 1;
                  CREATE UNIQUE NONCLUSTERED INDEX [{indexName}] ON [{_config.SchemaName}].[{_config.TableName}]([{nameof(DataProtectionKeyEntity.FriendlyName)}] ASC) WHERE [{nameof(DataProtectionKeyEntity.FriendlyName)}] IS NOT NULL ON [PRIMARY];
               END
               """,
            new { LockResource = InitializeDbLockResource },
            transaction);
        transaction.Commit();
    }
}