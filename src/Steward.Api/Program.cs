using System.Text.Json.Serialization;
using Asp.Versioning;
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
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
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

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
