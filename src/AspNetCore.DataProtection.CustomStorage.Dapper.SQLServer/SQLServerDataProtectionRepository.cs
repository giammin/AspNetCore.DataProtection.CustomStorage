using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;

/// <inheritdoc />
public class SQLServerDataProtectionRepository : IDbDataProtectionStorage
{
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
    public void InitializeDb()
    {
        var indexName = $"IX_{_config.TableName}_{nameof(DataProtectionKeyEntity.FriendlyName)}";
        using var connection = _connectionFactory.CreateConnection();
        // The unique index is ensured independently of the table, so that an existing table
        // without the index (created manually, or with the index dropped) gets it restored.
        connection.Execute(
            $"""
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
               """);
    }
}