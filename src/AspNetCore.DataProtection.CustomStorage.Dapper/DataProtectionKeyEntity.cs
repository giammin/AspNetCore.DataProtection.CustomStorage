using System;

namespace AspNetCore.DataProtection.CustomStorage.Dapper;

/// <summary>
/// the Entity representing the <see cref="DataProtectionKey"/>
/// saved on storage
/// </summary>
public record DataProtectionKeyEntity : DataProtectionKey
{
    /// <summary>
    /// primary key
    /// </summary>
    public int Id { get; set; }
    /// <summary>
    /// insertion date, set by the database when the key is stored.
    /// Tables created by the providers store it in UTC; SQL Server tables created by
    /// earlier versions store the server's local time.
    /// </summary>
    public DateTime InsertDate { get; set; }

}