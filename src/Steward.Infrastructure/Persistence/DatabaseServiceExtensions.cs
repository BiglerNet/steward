using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Steward.Infrastructure.Persistence;

public static class DatabaseServiceExtensions
{
    public static IServiceCollection AddStewardDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<StewardDbContext>(options =>
            options.UseNpgsql(BuildConnectionString(configuration), ConfigureNpgsql));

        return services;
    }

    // __EFMigrationsHistory lives outside the EF model (HasDefaultSchema doesn't cover it), so its
    // schema must be set explicitly here too — otherwise it's the one object PGO still won't let a
    // non-owner create in "public" on a fresh cluster, defeating the point of using our own schema.
    public static void ConfigureNpgsql(NpgsqlDbContextOptionsBuilder npgsqlOptions) =>
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", StewardDbContext.Schema);

    // DB_* vars come from the PGO-generated credential secret, whose password may contain
    // characters like ';' or '=' that corrupt a hand-concatenated connection string.
    // NpgsqlConnectionStringBuilder escapes them correctly; ConnectionStrings:DefaultConnection
    // remains as a single-string fallback for local dev / docker-compose.
    private static string BuildConnectionString(IConfiguration configuration)
    {
        var host = configuration["DB_HOST"];
        if (string.IsNullOrEmpty(host))
        {
            return configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = int.Parse(configuration["DB_PORT"] ?? "5432"),
            Database = configuration["DB_NAME"],
            Username = configuration["DB_USER"],
            Password = configuration["DB_PASSWORD"],
            GssEncryptionMode = GssEncryptionMode.Disable,
        };
        return builder.ConnectionString;
    }
}
