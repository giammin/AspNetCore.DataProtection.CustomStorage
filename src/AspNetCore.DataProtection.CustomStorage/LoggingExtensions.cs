using Microsoft.Extensions.Logging;

namespace AspNetCore.DataProtection.CustomStorage;

internal static partial class LoggingExtensions
{
    [LoggerMessage(1, LogLevel.Debug, "Reading data with key '{FriendlyName}', value '{Value}'.", EventName = "ReadKeyFromStorage")]
    public static partial void ReadingXmlFromKey(this ILogger logger, string? friendlyName, string value);

    [LoggerMessage(2, LogLevel.Debug, "Saving key '{FriendlyName}', value '{Value}' with '{storage}'.", EventName = "SavingKeyToStorage")]
    public static partial void LogSavingKeyToStorage(this ILogger logger, string? friendlyName, string value, string storage);

    [LoggerMessage(3, LogLevel.Error, "Error saving key '{FriendlyName}', value '{Value}' with '{storage}'.", EventName = "ErrorSavingKeyToStorage")]
    public static partial void LogErrorSavingKeyToStorage(this ILogger logger, string? friendlyName, string value, string storage);
}