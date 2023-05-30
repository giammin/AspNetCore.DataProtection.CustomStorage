namespace AspNetCore.DataProtection;

/// <summary>
/// key model
/// </summary>
public record DataProtectionKey
{
    /// <summary>
    /// The friendly name of the <see cref="DataProtectionKey"/>.
    /// It must be unique or null
    /// </summary>
    public string? FriendlyName { get; set; }

    /// <summary>
    /// The XML representation of the <see cref="DataProtectionKey"/>.
    /// </summary>
    public required string Xml { get; set; }
}