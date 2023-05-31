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
    /// insertion date
    /// </summary>
    public DateTime InsertDate { get; set; }

}