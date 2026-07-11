using System.Text.Json.Serialization;
using Asp.Versioning;
using Microsoft.AspNetCore.HttpOverrides;
using Steward.Api.ExceptionHandling;
using Steward.Api.OpenApi;
using Steward.Application;
using Steward.Infrastructure.Assets;
using Steward.Infrastructure.Dashboards;
using Steward.Infrastructure.Identity;
using Steward.Infrastructure.Persistence;
using Steward.Infrastructure.Setup;
using Steward.Infrastructure.Storage;
using Steward.Infrastructure.Tracking;
using Steward.Infrastructure.VinDecode;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

var isSetupMode = args.Contains("--setup");

builder.Services.AddStewardDatabase(builder.Configuration);
builder.Services.AddStewardAuth(builder.Configuration, registerHostedServices: !isSetupMode);

if (isSetupMode)
{
    builder.Services.AddHostedService<SetupHostedService>();
}
builder.Services.AddStewardAssets();
builder.Services.AddStewardTracking();
builder.Services.AddStewardDashboards();
builder.Services.AddStewardStorage(builder.Configuration);
builder.Services.AddStewardVinDecode();
builder.Services.AddApplication();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
              .AllowAnyHeader()
              .AllowAnyMethod());
});
builder.Services.AddHealthChecks()
    .AddDbContextCheck<StewardDbContext>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Trust any proxy in the cluster — Traefik's pod IP isn't fixed.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Steward API";
        options.AddHttpAuthentication("Bearer", scheme => { });
    });
}

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.Run();

public partial class Program;
