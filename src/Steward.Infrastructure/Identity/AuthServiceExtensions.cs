using System.Text;
using AspNet.Security.OAuth.Apple;
using Steward.Application.Auth;
using Steward.Application.Households;
using Steward.Application.Identity;
using Steward.Infrastructure.Authorization;
using Steward.Infrastructure.Households;
using Steward.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Steward.Infrastructure.Identity;

public static class AuthServiceExtensions
{
    public static IServiceCollection AddStewardAuth(
        this IServiceCollection services,
        IConfiguration configuration,
        bool registerHostedServices = true)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireNonAlphanumeric = true;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<StewardDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IHouseholdService, HouseholdService>();

        services.AddMemoryCache();
        services.AddSingleton<IOAuthExchangeService, MemoryCacheOAuthExchangeService>();

        services.AddSingleton<InvitationExpiryService>();
        services.AddSingleton<IInvitationExpiryService>(sp => sp.GetRequiredService<InvitationExpiryService>());
        if (registerHostedServices)
        {
            services.AddHostedService(sp => sp.GetRequiredService<InvitationExpiryService>());
        }

        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("Jwt:Key is required.");
        }

        var authBuilder = services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ClockSkew = TimeSpan.FromSeconds(30),
                };
            });

        var googleClientId = configuration["Auth:Google:ClientId"];
        if (!string.IsNullOrWhiteSpace(googleClientId))
        {
            authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.ClientId = googleClientId;
                options.ClientSecret = configuration["Auth:Google:ClientSecret"]!;
                options.CallbackPath = "/api/signin-google";
            });
        }

        var facebookClientId = configuration["Auth:Facebook:ClientId"];
        if (!string.IsNullOrWhiteSpace(facebookClientId))
        {
            authBuilder.AddFacebook(FacebookDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.AppId = facebookClientId;
                options.AppSecret = configuration["Auth:Facebook:ClientSecret"]!;
            });
        }

        var appleClientId = configuration["Auth:Apple:ClientId"];
        if (!string.IsNullOrWhiteSpace(appleClientId))
        {
            authBuilder.AddApple(AppleAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = IdentityConstants.ExternalScheme;
                options.ClientId = appleClientId;
                options.KeyId = configuration["Auth:Apple:KeyId"]!;
                options.TeamId = configuration["Auth:Apple:TeamId"]!;
            });
        }

        services.AddAuthorization();
        services.AddScoped<IAuthorizationHandler, HouseholdAuthorizationHandler>();
        if (registerHostedServices)
        {
            services.AddHostedService<PlatformAdminRoleSeeder>();
        }

        return services;
    }
}
