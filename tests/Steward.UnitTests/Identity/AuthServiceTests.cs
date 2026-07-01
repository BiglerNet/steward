using Steward.Application.Auth;
using Steward.Application.Identity;
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

        return new AuthService(_userManager, null!, _jwtTokenService, _oAuthExchangeService, _dbContext, config);
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
}
