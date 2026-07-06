using System.Net;
using System.Net.Http.Json;
using Steward.Application.Auth;
using Steward.Domain.Enums;
using Steward.IntegrationTests.Infrastructure;

namespace Steward.IntegrationTests.Auth;

public class AuthControllerThemePreferenceTests(DatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Register_Set_Theme_Then_Me_Reflects_The_Change()
    {
        var client = CreateAnonymousClient();
        var email = $"test-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Password123!", "Test User"),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(
            TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(registered);
        Assert.Null(registered!.User.ThemePreference);

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registered.Token);

        var updateResponse = await client.PatchAsJsonAsync(
            "/api/auth/me/theme",
            new UpdateThemePreferenceRequest(ThemePreference.Dark),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/auth/me", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        var me = await meResponse.Content.ReadFromJsonAsync<UserProfileResponse>(
            TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(ThemePreference.Dark, me!.ThemePreference);
    }

    [Fact]
    public async Task Update_Theme_Without_Token_Returns_Unauthorized()
    {
        var client = CreateAnonymousClient();

        var response = await client.PatchAsJsonAsync(
            "/api/auth/me/theme",
            new UpdateThemePreferenceRequest(ThemePreference.Dark),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_Theme_Value_Returns_BadRequest()
    {
        var client = CreateAnonymousClient();
        var email = $"test-{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest(email, "Password123!", "Test User"),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(
            TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registered!.Token);

        var response = await client.PatchAsJsonAsync(
            "/api/auth/me/theme",
            new UpdateThemePreferenceRequest((ThemePreference)99),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Updating_Theme_Does_Not_Affect_A_Different_User()
    {
        var clientA = CreateAnonymousClient();
        var registeredA = await (await clientA.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"test-{Guid.NewGuid():N}@example.com", "Password123!", "User A"),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        clientA.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registeredA!.Token);

        var clientB = CreateAnonymousClient();
        var registeredB = await (await clientB.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterRequest($"test-{Guid.NewGuid():N}@example.com", "Password123!", "User B"),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<AuthResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);
        clientB.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", registeredB!.Token);

        await clientA.PatchAsJsonAsync(
            "/api/auth/me/theme",
            new UpdateThemePreferenceRequest(ThemePreference.Dark),
            TestJson.Options,
            cancellationToken: TestContext.Current.CancellationToken);

        var meB = await (await clientB.GetAsync("/api/auth/me", TestContext.Current.CancellationToken))
            .Content.ReadFromJsonAsync<UserProfileResponse>(TestJson.Options, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(meB!.ThemePreference);
    }
}
