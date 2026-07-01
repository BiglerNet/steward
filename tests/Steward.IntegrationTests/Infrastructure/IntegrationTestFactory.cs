using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Steward.IntegrationTests.Infrastructure;

public class IntegrationTestFactory : WebApplicationFactory<Program>
{
    public const string ConnectionString =
        "Host=localhost;Port=5432;Database=steward_test;Username=steward;Password=steward";

    public const string JwtKey = "integration-test-signing-key-needs-32-chars-minimum";
    public const string JwtIssuer = "steward-api";
    public const string JwtAudience = "steward-clients";

    // Program.cs reads Jwt:Key eagerly while building services, before
    // ConfigureWebHost's ConfigureAppConfiguration hook is applied in the minimal
    // hosting model. Environment variables are picked up by WebApplication.CreateBuilder
    // itself, so set them ahead of any host build.
    static IntegrationTestFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Key", JwtKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
        Environment.SetEnvironmentVariable("Jwt__Audience", JwtAudience);
        Environment.SetEnvironmentVariable(
            "Storage__RootPath", Path.Combine(Path.GetTempPath(), "steward-tests", Guid.NewGuid().ToString("N")));
        Environment.SetEnvironmentVariable("Storage__MaxUploadSizeBytes", (10 * 1024 * 1024).ToString());
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }
}
