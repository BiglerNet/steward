using Steward.Application.Auth;
using Steward.Application.Identity;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Steward.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService,
    IOAuthExchangeService oAuthExchangeService,
    StewardDbContext dbContext,
    IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
        {
            throw new ConflictException("Email is already registered.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new ConflictException(string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        var adminEmail = configuration["PlatformAdmin:Email"];
        if (!string.IsNullOrEmpty(adminEmail) && string.Equals(adminEmail, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            await userManager.AddToRoleAsync(user, PlatformAdminRoleSeeder.RoleName);
        }

        var pendingInvites = await GetPendingInvitesAsync(request.Email, cancellationToken);
        return await BuildAuthResponseAsync(user, pendingInvites);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            throw new UnauthorizedException("Invalid email or password.");
        }

        return await BuildAuthResponseAsync(user, []);
    }

    public async Task<string> HandleOAuthCallbackAsync(
        string provider, string providerKey, string email, string? displayName, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByLoginAsync(provider, providerKey);
        if (user is null)
        {
            user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    DisplayName = displayName,
                    EmailConfirmed = true,
                };

                var createResult = await userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    throw new ConflictException(string.Join("; ", createResult.Errors.Select(e => e.Description)));
                }

                var adminEmail = configuration["PlatformAdmin:Email"];
                if (!string.IsNullOrEmpty(adminEmail) && string.Equals(adminEmail, email, StringComparison.OrdinalIgnoreCase))
                {
                    await userManager.AddToRoleAsync(user, PlatformAdminRoleSeeder.RoleName);
                }
            }

            await userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
        }

        return oAuthExchangeService.GenerateCode(user.Id);
    }

    public async Task<AuthResponse> ExchangeOAuthCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!oAuthExchangeService.TryRedeemCode(code, out var userId))
        {
            throw new BadRequestException("Exchange code is invalid or expired.");
        }

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new BadRequestException("Exchange code is invalid or expired.");

        var pendingInvites = await GetPendingInvitesAsync(user.Email!, cancellationToken);
        return await BuildAuthResponseAsync(user, pendingInvites);
    }

    private async Task<AuthResponse> BuildAuthResponseAsync(
        ApplicationUser user, IReadOnlyCollection<PendingInviteSummary> pendingInvites)
    {
        var roles = (await userManager.GetRolesAsync(user)).ToList();
        var expiryMinutes = configuration.GetValue<int?>("Jwt:ExpiryMinutes") ?? 15;
        var token = jwtTokenService.GenerateToken(new JwtTokenRequest(user.Id, user.Email!, user.DisplayName, roles));

        return new AuthResponse(
            token,
            DateTimeOffset.UtcNow.AddMinutes(expiryMinutes),
            new AuthenticatedUser(user.Id, user.Email!, user.DisplayName, user.ThemePreference),
            pendingInvites);
    }

    private async Task<IReadOnlyCollection<PendingInviteSummary>> GetPendingInvitesAsync(
        string email, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await dbContext.HouseholdInvitations
            .Where(i => i.Email == email && i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .Join(dbContext.Households, i => i.HouseholdId, h => h.Id, (i, h) => new PendingInviteSummary(
                i.InviteCode, h.Name, i.Role.ToString(), i.ExpiresAt))
            .ToListAsync(cancellationToken);
    }
}
