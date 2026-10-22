using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;

/// <inheritdoc />
public class PostgreSQLDataProtectionRepository : IDbDataProtectionStorage
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly DapperDataProtectionConfig _config;
    private string GetAllQuery() => 
    $"""
        SELECT
            id AS "{nameof(DataProtectionKeyEntity.Id)}",
            insert_date AS "{nameof(DataProtectionKeyEntity.InsertDate)}",
            friendly_name AS "{nameof(DataProtectionKeyEntity.FriendlyName)}",
            xml AS "{nameof(DataProtectionKeyEntity.Xml)}"
        FROM {_config.SchemaName}.{_config.TableName};
    """;

    /// <summary>
    /// create an instance of <see cref="PostgreSQLDataProtectionRepository"/>
    /// </summary>
    /// <param name="dataSource">the db connection</param>
    /// <param name="options">repository configuration</param>
    public PostgreSQLDataProtectionRepository(NpgsqlDataSource dataSource,
        IOptionsSnapshot<DapperDataProtectionConfig> options)
    {
        _dataSource = dataSource;
        _config = options.Value;
    }

    /// <inheritdoc />
    public IEnumerable<DataProtectionKey> GetAll()
    {
        using var connection = _dataSource.CreateConnection();
        return connection.Query<DataProtectionKeyEntity>(GetAllQuery());
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DataProtectionKeyEntity>> GetAllAsync()
    {
        await using var connection = _dataSource.CreateConnection();
        return await connection.QueryAsync<DataProtectionKeyEntity>(GetAllQuery());
    }

    /// <inheritdoc />
    public void Insert(DataProtectionKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var connection = _dataSource.CreateConnection();
        connection.Execute(
            $"""
             INSERT INTO {_config.SchemaName}.{_config.TableName}(
                friendly_name,
                xml) 
             VALUES (
                @{nameof(DataProtectionKeyEntity.FriendlyName)},
                @{nameof(DataProtectionKeyEntity.Xml)})
             """,
            key);
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

        using var connection = _dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        // Dapper executes the statement once per id, in order.
        connection.Execute(
            $"""
             DELETE FROM {_config.SchemaName}.{_config.TableName}
             WHERE id = @{nameof(DataProtectionKeyEntity.Id)}
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
    public void InitializeDb()
    {
        using var connection = _dataSource.CreateConnection();
        connection.Execute($"""
             CREATE TABLE IF NOT EXISTS {_config.SchemaName}.{_config.TableName}
              (
                 id INTEGER generated always as identity,
                 insert_date timestamp with time zone NOT NULL default NOW(),
                 friendly_name character varying(256) COLLATE pg_catalog."default" NULL,
                 xml text COLLATE pg_catalog."default" NOT NULL,
                 CONSTRAINT pk_{_config.SchemaName}_{_config.TableName} PRIMARY KEY (id)
              );
               
              CREATE UNIQUE INDEX IF NOT EXISTS ix_{_config.SchemaName}_{_config.TableName}_friendly_name
              ON {_config.SchemaName}.{_config.TableName} USING btree
              (friendly_name COLLATE pg_catalog."default" ASC NULLS LAST)
              TABLESPACE pg_default;
             """);
    }
}