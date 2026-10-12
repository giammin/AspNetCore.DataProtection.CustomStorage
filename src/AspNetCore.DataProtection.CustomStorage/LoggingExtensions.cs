using System;
using Microsoft.Extensions.Logging;

namespace AspNetCore.DataProtection.CustomStorage;

/// <summary>
/// High-performance logging extensions for data protection storage operations.
/// </summary>
internal static partial class LoggingExtensions
{
    /// <summary>
    /// Logs a debug message when reading a key from storage.
    /// The key XML is never logged because it may contain unencrypted key material.
    /// </summary>
    [LoggerMessage(1, LogLevel.Debug, "Reading data with key '{FriendlyName}'.", EventName = "ReadKeyFromStorage")]
    public static partial void ReadingXmlFromKey(this ILogger logger, string? friendlyName);

    /// <summary>
    /// Logs a debug message when saving a key to storage.
    /// The key XML is never logged because it may contain unencrypted key material.
    /// </summary>
    [LoggerMessage(2, LogLevel.Debug, "Saving key '{FriendlyName}' with '{storage}'.", EventName = "SavingKeyToStorage")]
    public static partial void LogSavingKeyToStorage(this ILogger logger, string? friendlyName, string storage);

    /// <summary>
    /// Logs an error message when saving a key to storage fails.
    /// The key XML is never logged because it may contain unencrypted key material.
    /// </summary>
    [LoggerMessage(3, LogLevel.Error, "Error saving key '{FriendlyName}' with '{storage}'.", EventName = "ErrorSavingKeyToStorage")]
    public static partial void LogErrorSavingKeyToStorage(this ILogger logger, Exception exception, string? friendlyName, string storage);
}
