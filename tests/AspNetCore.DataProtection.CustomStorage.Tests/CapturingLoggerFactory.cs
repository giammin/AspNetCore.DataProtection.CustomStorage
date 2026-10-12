using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AspNetCore.DataProtection.CustomStorage.Tests;

/// <summary>
/// Logger factory that records every log entry, including its structured state, for assertions.
/// </summary>
public sealed class CapturingLoggerFactory : ILoggerFactory
{
    public List<LogEntry> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(List<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var values = state as IEnumerable<KeyValuePair<string, object?>> ?? [];
            entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception), exception, values.ToList()));
        }
    }
}

/// <summary>
/// A recorded log entry.
/// </summary>
public sealed record LogEntry(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception,
    IReadOnlyList<KeyValuePair<string, object?>> State)
{
    /// <summary>
    /// True when the formatted message or any structured state value contains <paramref name="text"/>.
    /// </summary>
    public bool Contains(string text) =>
        Message.Contains(text, StringComparison.Ordinal) ||
        State.Any(kv => kv.Value?.ToString()?.Contains(text, StringComparison.Ordinal) == true);
}
