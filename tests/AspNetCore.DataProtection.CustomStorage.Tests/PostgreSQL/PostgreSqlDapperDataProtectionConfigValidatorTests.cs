using AspNetCore.DataProtection.CustomStorage.Dapper;
using AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;
using FluentAssertions;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests.PostgreSQL;

public class PostgreSqlDapperDataProtectionConfigValidatorTests
{
    private readonly DapperDataProtectionConfigValidator _sut = new();

    [Theory]
    [InlineData(Constants.DefaultSchema, Constants.TableName)]
    [InlineData("_schema1", "_keys_2")]
    [InlineData("security", "user")]
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
    public void Validate_MixedCaseName_FailsExplainingLowercaseRequirement()
    {
        var result = _sut.Validate(null, Config(Constants.DefaultSchema, "DataProtectionKeys"));

        result.FailureMessage.Should().Contain("lowercase");
    }

    [Theory]
    [InlineData(45, true)]
    [InlineData(46, false)]
    public void Validate_CombinedNameLength_LeavesRoomForDerivedIndexName(int combinedLength, bool succeeded)
    {
        var schemaName = new string('s', 10);
        var tableName = new string('t', combinedLength - schemaName.Length);

        PostgreSQLDataProtectionRepository.GetIndexName(schemaName, tableName).Length
            .Should().Be(succeeded ? DapperDataProtectionConfigValidator.MaxNameLength : DapperDataProtectionConfigValidator.MaxNameLength + 1);
        _sut.Validate(null, Config(schemaName, tableName)).Succeeded.Should().Be(succeeded);
        _sut.Validate(null, Config(tableName, schemaName)).Succeeded.Should().Be(succeeded);
    }

    [Fact]
    public void Validate_CombinedNameTooLong_FailsNamingBothOptions()
    {
        var result = _sut.Validate(null, Config(Constants.DefaultSchema, new string('t', 64)));

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain($"{nameof(DapperDataProtectionConfig)}.{nameof(DapperDataProtectionConfig.SchemaName)}")
            .And.Contain($"{nameof(DapperDataProtectionConfig)}.{nameof(DapperDataProtectionConfig.TableName)}")
            .And.Contain(DapperDataProtectionConfigValidator.MaxCombinedNameLength.ToString());
    }

    public static TheoryData<string?> InvalidNames() =>
    [
        null!,
        "",
        "   ",
        "1keys",
        "my-keys",
        "my keys",
        "Keys",
        "keys\"",
        "keys'",
        "public.keys",
        "keys\n",
        "chiav\u00e8"
    ];

    private static DapperDataProtectionConfig Config(string schemaName, string tableName) =>
        new() { SchemaName = schemaName, TableName = tableName };
}
