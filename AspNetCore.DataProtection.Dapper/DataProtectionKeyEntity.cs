namespace AspNetCore.DataProtection.Dapper;

public record DataProtectionKeyEntity : DataProtectionKey
{
    public int Id { get; set; }
    public DateTime InsertDate { get; set; }

}