using Microsoft.Extensions.Logging;

namespace AspNetCore.DataProtection.CustomStorage.Dapper;

/// <summary>
/// High-performance logging extensions for Dapper data protection storage operations.
/// </summary>
internal static partial class LoggingExtensions
{
    /// <summary>
    /// Logs a debug message during data protection initialization.
    /// </summary>
    [LoggerMessage(4, LogLevel.Debug, "UseDapperDataProtection config:'{Config}', storage: '{storage}'.", EventName = "InitializingDapperDataProtection")]
    public static partial void LogInitialization(this ILogger logger, string config, string storage);
}
