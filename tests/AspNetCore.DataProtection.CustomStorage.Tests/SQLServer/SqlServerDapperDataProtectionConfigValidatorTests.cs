using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.SQLServer;
using FluentAssertions;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests.SQLServer;

public class SqlServerDapperDataProtectionConfigValidatorTests
{
    private readonly DapperDataProtectionConfigValidator _sut = new();

    [Theory]
    [InlineData(Constants.DefaultSchema, Constants.TableName)]
    [InlineData("_Schema1", "_Keys_2")]
    [InlineData("Security", "Order")]
    public void Validate_ValidNames_Succeeds(string schemaName, string tableName)
    {
        var result = _sut.Validate(null, Config(schemaName, tableName));

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Validate_InvalidSchemaName_FailsNamingTheOption(string? schemaName)
    {
        var result = _sut.Validate(null, Config(schemaName!, Constants.TableName));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain($"{nameof(DapperDataProtectionConfig)}.{nameof(DapperDataProtectionConfig.SchemaName)}")
            .And.NotContain(nameof(DapperDataProtectionConfig.TableName));
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Validate_InvalidTableName_FailsNamingTheOption(string? tableName)
    {
        var result = _sut.Validate(null, Config(Constants.DefaultSchema, tableName!));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain($"{nameof(DapperDataProtectionConfig)}.{nameof(DapperDataProtectionConfig.TableName)}")
            .And.NotContain(nameof(DapperDataProtectionConfig.SchemaName));
    }

    [Fact]
    public void Validate_BothNamesInvalid_ReportsBoth()
    {
        var result = _sut.Validate(null, Config("my schema", "my-keys"));

        result.Failures.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(128, true)]
    [InlineData(129, false)]
    public void Validate_SchemaNameLength_IsLimitedToSqlServerIdentifierLength(int length, bool succeeded)
    {
        var result = _sut.Validate(null, Config(new string('s', length), Constants.TableName));

        result.Succeeded.Should().Be(succeeded);
    }

    [Theory]
    [InlineData(112, true)]
    [InlineData(113, false)]
    public void Validate_TableNameLength_LeavesRoomForDerivedIndexName(int length, bool succeeded)
    {
        var result = _sut.Validate(null, Config(Constants.DefaultSchema, new string('t', length)));

        result.Succeeded.Should().Be(succeeded);
    }

    public static TheoryData<string?> InvalidNames() =>
    [
        null!,
        "",
        "   ",
        "1Keys",
        "My-Keys",
        "My Keys",
        "Keys]",
        "Keys'",
        "Keys\"",
        "dbo.Keys",
        "Keys\n",
        "Chiav\u00e8"
    ];

    private static DapperDataProtectionConfig Config(string schemaName, string tableName) =>
        new() { SchemaName = schemaName, TableName = tableName };
}
