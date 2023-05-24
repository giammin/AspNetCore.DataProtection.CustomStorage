using System.ComponentModel.DataAnnotations;

namespace AspNetCore.DataProtection;

public record DataProtectionKey
{
    /// <summary>
    /// The friendly name of the <see cref="DataProtectionKey"/>.
    /// </summary>
    public string? FriendlyName { get; set; }

    /// <summary>
    /// The XML representation of the <see cref="DataProtectionKey"/>.
    /// </summary>
    public required string Xml { get; set; }
}