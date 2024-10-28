using System;
using Microsoft.Data.SqlClient;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;

/// <summary>
/// creates new sql connection
/// this class does not handle opening/closing and disposing the connection
/// </summary>
public class SqlConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="connectionString"></param>
    public SqlConnectionFactory(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    /// <summary>
    /// return a new and closed connection ready to be used
    /// </summary>
    /// <returns></returns>
    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}