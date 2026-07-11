using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Steward.Application.Auth;
using Steward.Infrastructure.Identity;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Auth;

public class AuthControllerRefreshTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private async Task<AuthResponse> RegisterAsync(HttpClient client)
    {
        var email = $"test-{Guid.NewGuid():N}@example.com";
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Password123!", "Test User"),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(
            TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        return body!;
    }

    [Fact]
    public async Task Register_Issues_Both_Access_And_Refresh_Tokens()
    {
        var client = CreateAnonymousClient();

        var registered = await RegisterAsync(client);

        Assert.False(string.IsNullOrEmpty(registered.Token));
        Assert.False(string.IsNullOrEmpty(registered.RefreshToken));
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesAndGrantsAccessWithNewToken()
    {
        var client = CreateAnonymousClient();
        var registered = await RegisterAsync(client);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(registered.RefreshToken),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(
            TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(refreshed);
        Assert.False(string.IsNullOrEmpty(refreshed!.Token));
        Assert.NotEqual(registered.RefreshToken, refreshed.RefreshToken);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.Token);
        var meResponse = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
    }

    [Fact]
    public async Task Refresh_UnknownToken_ReturnsUnauthorized()
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest("this-token-does-not-exist"),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_AlreadyRotatedToken_OutsideGraceWindow_ReturnsUnauthorized()
    {
        var client = CreateAnonymousClient();
        var registered = await RegisterAsync(client);

        var firstRefresh = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(registered.RefreshToken),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        using var scope = Factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<Steward.Infrastructure.Persistence.StewardDbContext>();
        var revokedRecord = await dbContext.RefreshTokens.SingleAsync(
            t => t.UserId == registered.User.Id && t.RevokedAt != null, TestContext.Current.CancellationToken);
        revokedRecord.RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

        var secondRefresh = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(registered.RefreshToken),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
    }

    [Fact]
    public async Task Logout_Revokes_Token_And_Subsequent_Refresh_Returns_Unauthorized()
    {
        var client = CreateAnonymousClient();
        var registered = await RegisterAsync(client);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.Token);
        var logoutResponse = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new LogoutRequest(registered.RefreshToken),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        client.DefaultRequestHeaders.Authorization = null;
        var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(registered.RefreshToken),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_Without_Authentication_Returns_Unauthorized()
    {
        var client = CreateAnonymousClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/logout",
            new LogoutRequest("some-token"),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RoleChangeSinceIssuance_IsReflectedInNewAccessToken()
    {
        var client = CreateAnonymousClient();
        var registered = await RegisterAsync(client);

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(registered.User.Id.ToString());
            await userManager.AddToRoleAsync(user!, PlatformAdminRoleSeeder.RoleName);
        }

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/auth/refresh",
            new RefreshRequest(registered.RefreshToken),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>(
            TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(refreshed!.Token);
        var roles = jwt.Claims.Where(c => c.Type == "role").Select(c => c.Value);
        Assert.Contains(PlatformAdminRoleSeeder.RoleName, roles);
    }
}
