using System.Linq;
using AspNetCore.DataProtection.CustomStorage.Dapper.PostgreSQL;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace AspNetCore.DataProtection.CustomStorage.Tests.PostgreSQL;

public class PostgreSqlDapperDataProtectionExtensionsTests
{
    [Theory]
    [InlineData("Host=localhost;Database=test", false)]
    [InlineData("Host=localhost;Database=test;Include Error Detail=false", false)]
    [InlineData("Host=localhost;Database=test;Include Error Detail=true", true)]
    public void PersistKeysWithDapperInPostgreSQL_PreservesIncludeErrorDetail(string connectionString, bool expected)
    {
        var services = new ServiceCollection();
        services.AddDataProtection().PersistKeysWithDapperInPostgreSQL(connectionString);

        var dataSource = services.Single(d => d.ServiceType == typeof(NpgsqlDataSource))
            .ImplementationInstance.Should().BeAssignableTo<NpgsqlDataSource>().Subject;

        new NpgsqlConnectionStringBuilder(dataSource.ConnectionString).IncludeErrorDetail
            .Should().Be(expected);
    }

    [Fact]
    public void PersistKeysWithDapperInPostgreSQL_DoesNotEnableMatchNamesWithUnderscores()
    {
        DefaultTypeMap.MatchNamesWithUnderscores = false;
        var services = new ServiceCollection();

        services.AddDataProtection().PersistKeysWithDapperInPostgreSQL("Host=localhost;Database=test");

        DefaultTypeMap.MatchNamesWithUnderscores.Should().BeFalse();
    }
}
