using EduGame.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace EduGame.Api.Tests;

public class DatabaseSettingsTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value))).Build();

    [Fact]
    public void Builds_connection_from_separate_fields()
    {
        var cs = DatabaseSettings.BuildConnectionString(Config(
            ("Database:Host", "db.exemplo.com"), ("Database:Port", "6543"), ("Database:Name", "jogo"),
            ("Database:Username", "app"), ("Database:Password", "s3nh@;com=especiais"), ("Database:SslMode", "require")));

        var parsed = new NpgsqlConnectionStringBuilder(cs);
        Assert.Equal("db.exemplo.com", parsed.Host);
        Assert.Equal(6543, parsed.Port);
        Assert.Equal("jogo", parsed.Database);
        Assert.Equal("app", parsed.Username);
        Assert.Equal("s3nh@;com=especiais", parsed.Password);
        Assert.Equal(SslMode.Require, parsed.SslMode);
    }

    [Fact]
    public void Port_defaults_to_5432()
    {
        var cs = DatabaseSettings.BuildConnectionString(Config(
            ("Database:Host", "h"), ("Database:Name", "n"), ("Database:Username", "u"), ("Database:Password", "p")));
        Assert.Equal(5432, new NpgsqlConnectionStringBuilder(cs).Port);
    }

    [Fact]
    public void Missing_password_gives_a_clear_error()
    {
        var e = Assert.Throws<InvalidOperationException>(() => DatabaseSettings.BuildConnectionString(Config(
            ("Database:Host", "h"), ("Database:Name", "n"), ("Database:Username", "u"))));
        Assert.Contains("DB_PASSWORD", e.Message);
    }

    [Fact]
    public void Falls_back_to_connection_string_when_host_is_not_set()
    {
        Assert.Equal("Host=x;Database=y", DatabaseSettings.BuildConnectionString(Config(("ConnectionStrings:Default", "Host=x;Database=y"))));
        Assert.Throws<InvalidOperationException>(() => DatabaseSettings.BuildConnectionString(Config()));
    }
}
