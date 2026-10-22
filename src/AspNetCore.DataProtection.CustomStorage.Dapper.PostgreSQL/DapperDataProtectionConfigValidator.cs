using System.Collections.Generic;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;

/// <summary>
/// Validates the schema and table names of <see cref="DapperDataProtectionConfig"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// Names must be lowercase. Identifiers are quoted, which makes them case-sensitive, while earlier versions
/// used unquoted identifiers that PostgreSQL folds to lowercase. Accepting a mixed-case name would silently
/// target a different, empty table instead of the one created by an earlier version.
/// </remarks>
internal sealed partial class DapperDataProtectionConfigValidator : IValidateOptions<DapperDataProtectionConfig>
{
    /// <summary>
    /// Maximum length of a PostgreSQL identifier (NAMEDATALEN - 1).
    /// </summary>
    internal const int MaxNameLength = 63;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DapperDataProtectionConfig options)
    {
        var failures = new List<string>();
        Validate(nameof(DapperDataProtectionConfig.SchemaName), options.SchemaName, failures);
        Validate(nameof(DapperDataProtectionConfig.TableName), options.TableName, failures);
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Validate(string optionName, string? value, List<string> failures)
    {
        if (value is null || value.Length > MaxNameLength || !IdentifierRegex().IsMatch(value))
        {
            failures.Add(
                $"{nameof(DapperDataProtectionConfig)}.{optionName} '{value}' is not a valid PostgreSQL name: " +
                "it must start with a lowercase letter or an underscore, contain only lowercase letters, digits " +
                $"and underscores, and be at most {MaxNameLength} characters long.");
        }
    }

    [GeneratedRegex(@"^[a-z_][a-z0-9_]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
