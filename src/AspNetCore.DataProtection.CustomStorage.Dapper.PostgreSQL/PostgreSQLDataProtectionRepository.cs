using Dapper;
using Microsoft.Extensions.Options;
using Npgsql;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;

/// <inheritdoc />
public class PostgreSQLDataProtectionRepository : IDbDataProtectionStorage
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly DapperDataProtectionConfig _config;

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
    public IEnumerable<DataProtectionKey> GetAll() => GetAllAsync().ConfigureAwait(false).GetAwaiter().GetResult();

    /// <inheritdoc />
    public async Task<IEnumerable<DataProtectionKeyEntity>> GetAllAsync()
    {
        await using var connection = _dataSource.CreateConnection();
        return await connection.QueryAsync<DataProtectionKeyEntity>(
            $"""
               SELECT
                    id,
                    insert_date,
                    friendly_name,
                    xml
               FROM {_config.SchemaName}.{_config.TableName}
            """);
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
    public void InitializeDb()
    {
        using var connection = _dataSource.CreateConnection();
        connection.Execute($"""
             CREATE TABLE IF NOT EXISTS {_config.SchemaName}.{_config.TableName}
              (
                 id INTEGER generated always as identity,
                 insert_date timestamp with time zone NOT NULL default NOW(),
                 friendly_name character varying(256) COLLATE pg_catalog."default" NOT NULL,
                 xml text COLLATE pg_catalog."default" NOT NULL,
                 CONSTRAINT pk_{_config.SchemaName}.{_config.TableName} PRIMARY KEY (id)
              );
               
              CREATE UNIQUE INDEX IF NOT EXISTS ix_{_config.SchemaName}.{_config.TableName}_friendly_name
              ON {_config.SchemaName}.{_config.TableName} USING btree
              (friendly_name COLLATE pg_catalog."default" ASC NULLS LAST)
              TABLESPACE pg_default;
             """);
    }
}