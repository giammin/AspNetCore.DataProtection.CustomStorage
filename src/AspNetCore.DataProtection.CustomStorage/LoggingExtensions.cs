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

    /// <summary>
    /// Logs an error message when a stored key cannot be parsed and is excluded from the key ring.
    /// Neither the key XML nor the parser exception is logged because both may contain unencrypted key material.
    /// </summary>
    [LoggerMessage(5, LogLevel.Error, "Skipping key '{FriendlyName}': its XML is malformed (line {LineNumber}, position {LinePosition}).", EventName = "SkippingMalformedKey")]
    public static partial void LogSkippingMalformedKey(this ILogger logger, string? friendlyName, int lineNumber, int linePosition);

    /// <summary>
    /// Logs an information message for each key deleted from storage, once the deletion has succeeded.
    /// The key XML is never logged because it may contain unencrypted key material.
    /// </summary>
    [LoggerMessage(6, LogLevel.Information, "Deleted key '{FriendlyName}' with '{storage}'.", EventName = "DeletedKeyFromStorage")]
    public static partial void LogDeletedKeyFromStorage(this ILogger logger, string? friendlyName, string storage);

    /// <summary>
    /// Logs an error message when deleting keys from storage fails.
    /// The key XML is never logged because it may contain unencrypted key material.
    /// </summary>
    [LoggerMessage(7, LogLevel.Error, "Error deleting {Count} key(s) with '{storage}'.", EventName = "ErrorDeletingKeysFromStorage")]
    public static partial void LogErrorDeletingKeysFromStorage(this ILogger logger, Exception exception, int count, string storage);
}
