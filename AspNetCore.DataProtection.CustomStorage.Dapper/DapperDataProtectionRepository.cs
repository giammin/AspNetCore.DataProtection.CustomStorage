using System.Data;
using Dapper;
using Microsoft.Extensions.Options;

namespace AspNetCore.DataProtection.CustomStorage.Dapper;

/// <inheritdoc />
public class DapperDataProtectionRepository: IDbDataProtectionStorage
{
    private readonly IDbConnection _connection;
    private readonly DapperDataProtectionConfig _config;

    /// <summary>
    /// create an instance of <see cref="DapperDataProtectionRepository"/>
    /// </summary>
    /// <param name="connection">the db connection</param>
    /// <param name="options">repository configuration</param>
    public DapperDataProtectionRepository(IDbConnection connection, IOptionsSnapshot<DapperDataProtectionConfig> options)
    {
        _connection = connection;
        _config = options.Value;
    }

    /// <inheritdoc />
    IEnumerable<DataProtectionKey> IDataProtectionStorage.GetAll() => GetAll();

    /// <summary>
    /// return all the <see cref="DataProtectionKeyEntity"/> saved
    /// </summary>
    /// <returns></returns>
    public IEnumerable<DataProtectionKeyEntity> GetAll() => _connection.Query<DataProtectionKeyEntity>($"select * from [{_config.SchemaName}].[{_config.TableName}]");

    /// <inheritdoc />
    public void Insert(DataProtectionKey key)
    {
        ArgumentNullException.ThrowIfNull(key);

        _connection.Execute(
            $"INSERT INTO [{_config.SchemaName}].[{_config.TableName}]({nameof(DataProtectionKeyEntity.FriendlyName)}, {nameof(DataProtectionKeyEntity.Xml)}) VALUES (@{nameof(DataProtectionKeyEntity.FriendlyName)}, @{nameof(DataProtectionKeyEntity.Xml)})",
            key);
    }
    /// <summary>
    /// create the keys table if needed
    /// </summary>
    public void InitializeDb()
    {
        _connection.Execute($"""
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