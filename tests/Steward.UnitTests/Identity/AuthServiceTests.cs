using Steward.Application.Auth;
using Steward.Application.Identity;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Enums;
using Steward.Infrastructure.Identity;
using Steward.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NSubstitute;

namespace Steward.UnitTests.Identity;

public class AuthServiceTests
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IOAuthExchangeService _oAuthExchangeService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly StewardDbContext _dbContext;

    public AuthServiceTests()
    {
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            Substitute.For<IUserStore<ApplicationUser>>(),
            null, null, null, null, null, null, null, null);

        _userManager.FindByEmailAsync(Arg.Any<string>())
            .Returns(Task.FromResult<ApplicationUser?>(null));
        _userManager.CreateAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.CreateAsync(Arg.Any<ApplicationUser>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.AddToRoleAsync(Arg.Any<ApplicationUser>(), Arg.Any<string>())
            .Returns(Task.FromResult(IdentityResult.Success));
        _userManager.GetRolesAsync(Arg.Any<ApplicationUser>())
            .Returns(Task.FromResult<IList<string>>(new List<string>()));
        _userManager.FindByLoginAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.FromResult<ApplicationUser?>(null));
        _userManager.AddLoginAsync(Arg.Any<ApplicationUser>(), Arg.Any<UserLoginInfo>())
            .Returns(Task.FromResult(IdentityResult.Success));

        _jwtTokenService = Substitute.For<IJwtTokenService>();
        _jwtTokenService.GenerateToken(Arg.Any<JwtTokenRequest>()).Returns("test-token");

        _oAuthExchangeService = Substitute.For<IOAuthExchangeService>();
        _oAuthExchangeService.GenerateCode(Arg.Any<Guid>()).Returns("test-code");

        _refreshTokenService = Substitute.For<IRefreshTokenService>();
        _refreshTokenService.IssueAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenIssueResult("test-refresh-token", DateTimeOffset.UtcNow.AddDays(30)));

        var dbOptions = new DbContextOptionsBuilder<StewardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new StewardDbContext(dbOptions);
    }

    private AuthService CreateService(string adminEmail = "")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PlatformAdmin:Email"] = adminEmail,
                ["Jwt:ExpiryMinutes"] = "15",
            })
            .Build();

        return new AuthService(_userManager, null!, _jwtTokenService, _oAuthExchangeService, _refreshTokenService, _dbContext, config);
    }

    // --- RegisterAsync ---

    [Fact]
    public async Task RegisterAsync_MatchingAdminEmail_GrantsPlatformAdminRole()
    {
        var service = CreateService(adminEmail: "admin@example.com");

        await service.RegisterAsync(new RegisterRequest("admin@example.com", "Password123!", "Admin"), TestContext.Current.CancellationToken);

        await _userManager.Received(1).AddToRoleAsync(
            Arg.Any<ApplicationUser>(),
            PlatformAdminRoleSeeder.RoleName);
    }

    [Fact]
    public async Task RegisterAsync_MatchingAdminEmail_IsCaseInsensitive()
    {
        var service = CreateService(adminEmail: "Admin@Example.com");

        await service.RegisterAsync(new RegisterRequest("admin@example.com", "Password123!", "Admin"), TestContext.Current.CancellationToken);

        await _userManager.Received(1).AddToRoleAsync(
            Arg.Any<ApplicationUser>(),
            PlatformAdminRoleSeeder.RoleName);
    }

    [Fact]
    public async Task RegisterAsync_NonMatchingEmail_DoesNotGrantPlatformAdminRole()
    {
        var service = CreateService(adminEmail: "admin@example.com");

        await service.RegisterAsync(new RegisterRequest("other@example.com", "Password123!", "Other"), TestContext.Current.CancellationToken);

        await _userManager.DidNotReceive().AddToRoleAsync(
            Arg.Any<ApplicationUser>(),
            Arg.Any<string>());
    }

    [Fact]
    public async Task RegisterAsync_EmptyAdminEmail_DoesNotGrantPlatformAdminRole()
    {
        var service = CreateService(adminEmail: "");

        await service.RegisterAsync(new RegisterRequest("admin@example.com", "Password123!", "Admin"), TestContext.Current.CancellationToken);

        await _userManager.DidNotReceive().AddToRoleAsync(
            Arg.Any<ApplicationUser>(),
            Arg.Any<string>());
    }

    // --- HandleOAuthCallbackAsync ---

    [Fact]
    public async Task HandleOAuthCallbackAsync_NewUserWithMatchingEmail_GrantsPlatformAdminRole()
    {
        var service = CreateService(adminEmail: "admin@example.com");

        await service.HandleOAuthCallbackAsync("Google", "google-key-123", "admin@example.com", "Admin", TestContext.Current.CancellationToken);

        await _userManager.Received(1).AddToRoleAsync(
            Arg.Any<ApplicationUser>(),
            PlatformAdminRoleSeeder.RoleName);
    }

    [Fact]
    public async Task HandleOAuthCallbackAsync_ExistingUserWithMatchingEmail_DoesNotGrantPlatformAdminRole()
    {
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "admin@example.com",
            UserName = "admin@example.com",
        };
        _userManager.FindByLoginAsync("Google", "google-key-123")
            .Returns(Task.FromResult<ApplicationUser?>(existingUser));

        var service = CreateService(adminEmail: "admin@example.com");

        await service.HandleOAuthCallbackAsync("Google", "google-key-123", "admin@example.com", "Admin", TestContext.Current.CancellationToken);

        await _userManager.DidNotReceive().AddToRoleAsync(
            Arg.Any<ApplicationUser>(),
            Arg.Any<string>());
    }

    // --- ThemePreference propagation ---

    [Fact]
    public async Task RegisterAsync_NewUser_ReturnsNullThemePreference()
    {
        var service = CreateService();

        var response = await service.RegisterAsync(
            new RegisterRequest("new@example.com", "Password123!", "New User"), TestContext.Current.CancellationToken);

        Assert.Null(response.User.ThemePreference);
    }

    [Fact]
    public async Task ExchangeOAuthCodeAsync_UserWithStoredThemePreference_ReturnsIt()
    {
        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            UserName = "user@example.com",
            ThemePreference = ThemePreference.Dark,
        };
        _oAuthExchangeService.TryRedeemCode("valid-code", out Arg.Any<Guid>())
            .Returns(x =>
            {
                x[1] = existingUser.Id;
                return true;
            });
        _userManager.FindByIdAsync(existingUser.Id.ToString())
            .Returns(Task.FromResult<ApplicationUser?>(existingUser));

        var service = CreateService();

        var response = await service.ExchangeOAuthCodeAsync("valid-code", TestContext.Current.CancellationToken);

        Assert.Equal(ThemePreference.Dark, response.User.ThemePreference);
    }

    // --- RefreshAsync ---

    [Fact]
    public async Task RefreshAsync_ReDerivesRolesFromDatabase()
    {
        var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com", UserName = "user@example.com" };
        _refreshTokenService.RotateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenRotateResult(user.Id, "new-refresh-token", DateTimeOffset.UtcNow.AddDays(30)));
        _userManager.FindByIdAsync(user.Id.ToString())
            .Returns(Task.FromResult<ApplicationUser?>(user));
        _userManager.GetRolesAsync(user)
            .Returns(Task.FromResult<IList<string>>(new List<string> { "PlatformAdmin" }));

        var service = CreateService();

        var response = await service.RefreshAsync(new RefreshRequest("old-token"), TestContext.Current.CancellationToken);

        _jwtTokenService.Received(1).GenerateToken(Arg.Is<JwtTokenRequest>(r => r.Roles.Contains("PlatformAdmin")));
        Assert.Equal("new-refresh-token", response.RefreshToken);
    }

    [Fact]
    public async Task RefreshAsync_InvalidToken_ThrowsUnauthorized()
    {
        _refreshTokenService.RotateAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((RefreshTokenRotateResult?)null);

        var service = CreateService();

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            service.RefreshAsync(new RefreshRequest("bad-token"), TestContext.Current.CancellationToken));
    }

    // --- LogoutAsync ---

    [Fact]
    public async Task LogoutAsync_RevokesTokenChain()
    {
        var service = CreateService();

        await service.LogoutAsync(new LogoutRequest("some-token"), TestContext.Current.CancellationToken);

        await _refreshTokenService.Received(1).RevokeChainAsync("some-token", Arg.Any<CancellationToken>());
    }
}
