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

    /// <summary>
    /// Maximum combined length of the schema and table names, which leaves room for the derived index name
    /// <c>ix_{SchemaName}_{TableName}_friendly_name</c>, the longest name derived from them.
    /// PostgreSQL would silently truncate a longer name, so two tables could get the same index name.
    /// </summary>
    internal static readonly int MaxCombinedNameLength =
        MaxNameLength - PostgreSQLDataProtectionRepository.GetIndexName(string.Empty, string.Empty).Length;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, DapperDataProtectionConfig options)
    {
        var failures = new List<string>();
        Validate(nameof(DapperDataProtectionConfig.SchemaName), options.SchemaName, failures);
        Validate(nameof(DapperDataProtectionConfig.TableName), options.TableName, failures);
        if (failures.Count == 0 && options.SchemaName.Length + options.TableName.Length > MaxCombinedNameLength)
        {
            failures.Add(
                $"{nameof(DapperDataProtectionConfig)}.{nameof(DapperDataProtectionConfig.SchemaName)} '{options.SchemaName}' " +
                $"and {nameof(DapperDataProtectionConfig)}.{nameof(DapperDataProtectionConfig.TableName)} '{options.TableName}' " +
                $"are too long: together they must be at most {MaxCombinedNameLength} characters long, so that the " +
                $"derived index name fits the PostgreSQL limit of {MaxNameLength} characters.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Validate(string optionName, string? value, List<string> failures)
    {
        if (value is null || !IdentifierRegex().IsMatch(value))
        {
            failures.Add(
                $"{nameof(DapperDataProtectionConfig)}.{optionName} '{value}' is not a valid PostgreSQL name: " +
                "it must start with a lowercase letter or an underscore and contain only lowercase letters, digits " +
                "and underscores.");
        }
    }

    [GeneratedRegex(@"^[a-z_][a-z0-9_]*\z", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
