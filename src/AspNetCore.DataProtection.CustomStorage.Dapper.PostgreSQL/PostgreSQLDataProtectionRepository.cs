using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;

/// <inheritdoc />
public class PostgreSQLDataProtectionRepository : IDbDataProtectionStorage
{
    // Database-wide: initializations of different tables are serialized too, which is harmless.
    private const string InitializeDbLockName = "AspNetCore.DataProtection.CustomStorage.InitializeDb";
    private readonly NpgsqlDataSource _dataSource;
    private readonly DapperDataProtectionConfig _config;
    private readonly string _tableName;
    private string GetAllQuery() => 
    $"""
        SELECT
            id AS "{nameof(DataProtectionKeyEntity.Id)}",
            insert_date AS "{nameof(DataProtectionKeyEntity.InsertDate)}",
            friendly_name AS "{nameof(DataProtectionKeyEntity.FriendlyName)}",
            xml AS "{nameof(DataProtectionKeyEntity.Xml)}"
        FROM {_tableName};
    """;

    /// <summary>
    /// create an instance of <see cref="PostgreSQLDataProtectionRepository"/>
    /// </summary>
    /// <param name="dataSource">the db connection</param>
    /// <param name="options">repository configuration</param>
    public PostgreSQLDataProtectionRepository([FromKeyedServices(Constants.DataSourceServiceKey)] NpgsqlDataSource dataSource,
        IOptionsSnapshot<DapperDataProtectionConfig> options)
    {
        _dataSource = dataSource;
        _config = options.Value;
        _tableName = $"{QuoteIdentifier(_config.SchemaName)}.{QuoteIdentifier(_config.TableName)}";
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
             INSERT INTO {_tableName}(
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
             DELETE FROM {_tableName}
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
    /// <remarks>
    /// Creates the schema, the table and the friendly-name unique index, each one only if it is missing.
    /// Safe to call concurrently from several instances: the initialization is serialized
    /// with a transaction-level advisory lock.
    /// </remarks>
    public void InitializeDb()
    {
        using var connection = _dataSource.OpenConnection();
        using var transaction = connection.BeginTransaction();
        // Without the lock, concurrent IF NOT EXISTS statements can all create the same relation
        // and fail with a unique violation in the system catalogs.
        connection.Execute("SELECT pg_advisory_xact_lock(hashtext(@LockName));",
            new { LockName = InitializeDbLockName },
            transaction);
        // CREATE SCHEMA IF NOT EXISTS requires the CREATE privilege on the database even when the schema
        // exists, so it is executed only when the schema is missing.
        var schemaExists = connection.ExecuteScalar<bool>(
            "SELECT EXISTS(SELECT 1 FROM pg_catalog.pg_namespace WHERE nspname = @SchemaName);",
            new { _config.SchemaName },
            transaction);
        if (!schemaExists)
        {
            connection.Execute($"CREATE SCHEMA IF NOT EXISTS {QuoteIdentifier(_config.SchemaName)};",
                transaction: transaction);
        }

        connection.Execute($"""
             CREATE TABLE IF NOT EXISTS {_tableName}
              (
                 id INTEGER generated always as identity,
                 insert_date timestamp with time zone NOT NULL default NOW(),
                 friendly_name character varying(256) COLLATE pg_catalog."default" NULL,
                 xml text COLLATE pg_catalog."default" NOT NULL,
                 CONSTRAINT {QuoteIdentifier($"pk_{_config.SchemaName}_{_config.TableName}")} PRIMARY KEY (id)
              );
               
              CREATE UNIQUE INDEX IF NOT EXISTS {QuoteIdentifier($"ix_{_config.SchemaName}_{_config.TableName}_friendly_name")}
              ON {_tableName} USING btree
              (friendly_name COLLATE pg_catalog."default" ASC NULLS LAST)
              TABLESPACE pg_default;
             """,
            transaction: transaction);
        transaction.Commit();
    }

    private static string QuoteIdentifier(string name) => $"\"{name.Replace("\"", "\"\"")}\"";
}