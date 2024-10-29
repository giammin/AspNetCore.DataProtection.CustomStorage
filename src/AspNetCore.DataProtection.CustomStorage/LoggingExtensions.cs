using System;
using Microsoft.Extensions.Logging;

namespace AspNetCore.DataProtection.CustomStorage;

/// <summary>
/// 
/// </summary>
public static partial class LoggingExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="friendlyName"></param>
    /// <param name="value"></param>
    [LoggerMessage(1, LogLevel.Debug, "Reading data with key '{FriendlyName}', value '{Value}'.", EventName = "ReadKeyFromStorage")]
    public static partial void ReadingXmlFromKey(this ILogger logger, string? friendlyName, string value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="friendlyName"></param>
    /// <param name="value"></param>
    /// <param name="storage"></param>
    [LoggerMessage(2, LogLevel.Debug, "Saving key '{FriendlyName}', value '{Value}' with '{storage}'.", EventName = "SavingKeyToStorage")]
    public static partial void LogSavingKeyToStorage(this ILogger logger, string? friendlyName, string value, string storage);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="friendlyName"></param>
    /// <param name="value"></param>
    /// <param name="storage"></param>
    [LoggerMessage(3, LogLevel.Error, "Error saving key '{FriendlyName}', value '{Value}' with '{storage}'.", EventName = "ErrorSavingKeyToStorage")]
    public static partial void LogErrorSavingKeyToStorage(this ILogger logger, string? friendlyName, string value, string storage);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="config"></param>
    /// <param name="storage"></param>
    [LoggerMessage(4, LogLevel.Debug, "UseDapperDataProtection config:'{Config}', storage: '{storage}'.", EventName = "InitializingDapperDataProtection")]
    public static partial void LogInitialization(this ILogger logger, string config, string storage);
}