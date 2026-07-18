using Steward.Domain.Entities;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Steward.Infrastructure.Maintenance;

public class PlatformTemplateSeeder(IServiceProvider serviceProvider) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<StewardDbContext>();

        foreach (var template in SeedTemplates)
        {
            var exists = await dbContext.Templates.AsNoTracking()
                .AnyAsync(t => t.Id == template.Id, cancellationToken);

            if (!exists)
            {
                dbContext.Templates.Add(template);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IReadOnlyList<Template> SeedTemplates =>
    [
        new Template
        {
            Id = Guid.Parse("00000000-0000-0000-0001-000000000001"),
            HouseholdId = null,
            Title = "Oil change",
            Description = "Routine engine oil and filter change.",
            ApplicableCategories =
            [
                AssetCategory.Car, AssetCategory.Truck, AssetCategory.Suv, AssetCategory.Van,
                AssetCategory.PowerBoat, AssetCategory.Pwc,
            ],
            Steps =
            [
                new TemplateStep
                {
                    Id = Guid.Parse("00000000-0000-0000-0001-000000000101"),
                    Text = "Change engine oil and filter",
                    SortOrder = 0,
                    EngineScoped = true,
                    RecurrenceIntervalMonths = 6,
                    RecurrenceIntervalMiles = 5000,
                    SuggestedParts =
                    [
                        new TemplateStepSuggestedPart
                        {
                            Id = Guid.Parse("00000000-0000-0000-0001-000000000201"),
                            Name = "Oil filter",
                            Quantity = 1,
                            SortOrder = 0,
                        },
                        new TemplateStepSuggestedPart
                        {
                            Id = Guid.Parse("00000000-0000-0000-0001-000000000202"),
                            Name = "Engine oil",
                            Quantity = 5,
                            SortOrder = 1,
                        },
                    ],
                },
                new TemplateStep
                {
                    Id = Guid.Parse("00000000-0000-0000-0001-000000000102"),
                    Text = "Check oil level after first run",
                    SortOrder = 1,
                    EngineScoped = true,
                },
            ],
        },
        new Template
        {
            Id = Guid.Parse("00000000-0000-0000-0002-000000000001"),
            HouseholdId = null,
            Title = "Tire rotation",
            Description = "Rotate tires to promote even wear.",
            ApplicableCategories = [AssetCategory.Car, AssetCategory.Truck, AssetCategory.Suv, AssetCategory.Van],
            Steps =
            [
                new TemplateStep
                {
                    Id = Guid.Parse("00000000-0000-0000-0002-000000000101"),
                    Text = "Rotate tires",
                    SortOrder = 0,
                    EngineScoped = false,
                    RecurrenceIntervalMonths = 6,
                    RecurrenceIntervalMiles = 6000,
                },
            ],
        },
        new Template
        {
            Id = Guid.Parse("00000000-0000-0000-0003-000000000001"),
            HouseholdId = null,
            Title = "Winterize engine",
            Description = "Prepare an engine for cold-weather storage.",
            ApplicableCategories =
            [
                AssetCategory.PowerBoat, AssetCategory.Sailboat, AssetCategory.Pwc, AssetCategory.Snowmobile,
            ],
            Steps =
            [
                new TemplateStep
                {
                    Id = Guid.Parse("00000000-0000-0000-0003-000000000101"),
                    Text = "Fog engine cylinders",
                    SortOrder = 0,
                    EngineScoped = true,
                },
                new TemplateStep
                {
                    Id = Guid.Parse("00000000-0000-0000-0003-000000000102"),
                    Text = "Add fuel stabilizer",
                    SortOrder = 1,
                    EngineScoped = true,
                    SuggestedParts =
                    [
                        new TemplateStepSuggestedPart
                        {
                            Id = Guid.Parse("00000000-0000-0000-0003-000000000201"),
                            Name = "Fuel stabilizer",
                            Quantity = 1,
                            SortOrder = 0,
                        },
                    ],
                },
                new TemplateStep
                {
                    Id = Guid.Parse("00000000-0000-0000-0003-000000000103"),
                    Text = "Drain and flush water system",
                    SortOrder = 2,
                    EngineScoped = false,
                },
            ],
        },
        new Template
        {
            Id = Guid.Parse("00000000-0000-0000-0004-000000000001"),
            HouseholdId = null,
            Title = "Battery check",
            Description = "Test and inspect the battery.",
            ApplicableCategories = [],
            Steps =
            [
                new TemplateStep
                {
                    Id = Guid.Parse("00000000-0000-0000-0004-000000000101"),
                    Text = "Test battery voltage and clean terminals",
                    SortOrder = 0,
                    EngineScoped = false,
                    RecurrenceIntervalMonths = 6,
                },
            ],
        },
    ];
}
