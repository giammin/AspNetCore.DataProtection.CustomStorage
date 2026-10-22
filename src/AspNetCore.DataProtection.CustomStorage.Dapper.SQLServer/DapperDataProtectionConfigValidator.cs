using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;

/// <summary>
/// Validates the schema and table names of <see cref="DapperDataProtectionConfig"/> for SQL Server.
/// </summary>
internal sealed partial class DapperDataProtectionConfigValidator : IValidateOptions<DapperDataProtectionConfig>
{
    /// <summary>
    /// Maximum length of a SQL Server identifier (sysname).
    /// </summary>
    internal const int MaxSchemaNameLength = 128;

    /// <summary>
    /// Maximum table name length, which leaves room for the derived index name <c>IX_{TableName}_FriendlyName</c>,
    /// the longest name derived from the table name.
    /// </summary>
    internal static readonly int MaxTableNameLength =
        MaxSchemaNameLength - SQLServerDataProtectionRepository.GetIndexName(string.Empty).Length;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DapperDataProtectionConfig options)
    {
        var failures = new List<string>();
        Validate(nameof(DapperDataProtectionConfig.SchemaName), options.SchemaName, MaxSchemaNameLength, failures);
        Validate(nameof(DapperDataProtectionConfig.TableName), options.TableName, MaxTableNameLength, failures);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Validate(string optionName, string? value, int maxLength, List<string> failures)
    {
        if (value is null || value.Length > maxLength || !IdentifierRegex().IsMatch(value))
        {
            failures.Add(
                $"{nameof(DapperDataProtectionConfig)}.{optionName} '{value}' is not a valid SQL Server name: " +
                "it must start with a letter or an underscore, contain only letters, digits and underscores, " +
                $"and be at most {maxLength} characters long.");
        }
    }

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
