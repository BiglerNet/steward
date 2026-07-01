using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Auth;
using Steward.Application.Households;
using Steward.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IAuthService authService,
    IHouseholdService householdService,
    IValidator<RegisterRequest> registerValidator,
    IValidator<LoginRequest> loginValidator,
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration) : ControllerBase
{
    private static readonly string[] SupportedProviders = ["google", "facebook", "apple"];

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var response = await authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var response = await authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("oauth/{provider}/login")]
    public IActionResult OAuthLogin(string provider)
    {
        if (!SupportedProviders.Contains(provider, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest($"Unsupported provider '{provider}'.");
        }

        var redirectUri = Url.Action(nameof(OAuthCallback), "Auth", new { provider }, Request.Scheme)!;
        var properties = new AuthenticationProperties { RedirectUri = redirectUri };
        return Challenge(properties, NormalizeProviderScheme(provider));
    }

    [HttpGet("oauth/{provider}/callback")]
    public async Task<IActionResult> OAuthCallback(string provider, CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
        if (!result.Succeeded || result.Principal is null)
        {
            return BadRequest("External authentication failed.");
        }

        var providerKey = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("External login did not return a provider key.");
        var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
            ?? throw new InvalidOperationException("External login did not return an email.");
        var displayName = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        var exchangeCode = await authService.HandleOAuthCallbackAsync(
            NormalizeProviderScheme(provider), providerKey, email, displayName, cancellationToken);

        var frontendBaseUrl = configuration["Frontend:BaseUrl"];
        return Redirect($"{frontendBaseUrl}/auth/callback?code={exchangeCode}");
    }

    [HttpPost("oauth/exchange")]
    public async Task<IActionResult> OAuthExchange(OAuthExchangeRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.ExchangeOAuthCodeAsync(request.Code, cancellationToken);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = User.GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserProfileResponse(user.Id, user.Email!, user.DisplayName, user.AvatarUrl));
    }

    [Authorize]
    [HttpPost("invites/{code}/accept")]
    public async Task<IActionResult> AcceptInvite(string code, CancellationToken cancellationToken)
    {
        await householdService.AcceptInviteAsync(User.GetUserId(), code, cancellationToken);
        return Ok();
    }

    private static string NormalizeProviderScheme(string provider) =>
        char.ToUpperInvariant(provider[0]) + provider[1..].ToLowerInvariant();
}
