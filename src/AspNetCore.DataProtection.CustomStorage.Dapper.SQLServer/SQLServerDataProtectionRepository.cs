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
    public IEnumerable<DataProtectionKey> GetAll() => GetAllAsync().ConfigureAwait(false).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<IEnumerable<DataProtectionKeyEntity>> GetAllAsync()
    {
        await using var connection = _connectionFactory.CreateConnection();
        return await connection.QueryAsync<DataProtectionKeyEntity>(
            $"""
                SELECT
                    [{nameof(DataProtectionKeyEntity.Id)}],
                    [{nameof(DataProtectionKeyEntity.InsertDate)}],
                    [{nameof(DataProtectionKeyEntity.FriendlyName)}],
                    [{nameof(DataProtectionKeyEntity.Xml)}]
                FROM [{_config.SchemaName}].[{_config.TableName}]
                """);
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
        using var connection = _connectionFactory.CreateConnection();
        connection.Execute(
            $"""
               IF NOT EXISTS(SELECT t.[object_id] 
               FROM [sys].[tables] t
                  JOIN sys.schemas s ON s.schema_id=t.schema_id
               WHERE t.[name] = N'{_config.TableName}' AND s.name=N'{_config.SchemaName}')
               BEGIN
                   CREATE TABLE [{_config.SchemaName}].[{_config.TableName}](
                       [{nameof(DataProtectionKeyEntity.Id)}] [int] IDENTITY(1,1) NOT NULL,
                       [{nameof(DataProtectionKeyEntity.InsertDate)}] [datetime] NOT NULL default getdate(),
                      [{nameof(DataProtectionKeyEntity.FriendlyName)}] [nvarchar](256) NULL,
                      [{nameof(DataProtectionKeyEntity.Xml)}] [nvarchar](max) NOT NULL,
                      CONSTRAINT [PK_{_config.TableName}] PRIMARY KEY CLUSTERED ([{nameof(DataProtectionKeyEntity.Id)}] ASC) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                  ) ON [PRIMARY];
                  CREATE UNIQUE NONCLUSTERED INDEX [IX_{_config.TableName}_{nameof(DataProtectionKeyEntity.FriendlyName)}] ON [{_config.SchemaName}].[{_config.TableName}]([{nameof(DataProtectionKeyEntity.FriendlyName)}] ASC) WHERE [{nameof(DataProtectionKeyEntity.FriendlyName)}] IS NOT NULL WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY];
               END
               """);
    }
}