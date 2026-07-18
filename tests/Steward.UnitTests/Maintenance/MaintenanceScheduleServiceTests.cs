using Steward.Domain.Entities;
using Steward.Domain.Enums;
using Steward.Infrastructure.Maintenance;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Steward.UnitTests.Maintenance;

public class MaintenanceScheduleServiceTests
{
    private readonly StewardDbContext _dbContext;
    private readonly MaintenanceScheduleService _service;

    public MaintenanceScheduleServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<StewardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new StewardDbContext(dbOptions);
        _service = new MaintenanceScheduleService(_dbContext);
    }

    private static DateTimeOffset AsResolvedAt(DateOnly date) =>
        new(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private (Template Template, TemplateStep Step) SeedTemplateStep(
        bool engineScoped = false,
        int? intervalMonths = null,
        decimal? intervalMiles = null,
        decimal? intervalHours = null)
    {
        var template = new Template { Id = Guid.NewGuid(), Title = "Winterization" };
        var step = new TemplateStep
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            Text = "Change oil",
            EngineScoped = engineScoped,
            RecurrenceIntervalMonths = intervalMonths,
            RecurrenceIntervalMiles = intervalMiles,
            RecurrenceIntervalHours = intervalHours,
        };
        _dbContext.Templates.Add(template);
        _dbContext.TemplateSteps.Add(step);
        return (template, step);
    }

    private ChecklistItem SeedChecklistItem(
        Guid assetId,
        Guid templateStepId,
        Guid? engineId,
        ChecklistItemStatus status,
        DateOnly? resolvedOn)
    {
        var maintenanceItem = new MaintenanceItem
        {
            Id = Guid.NewGuid(),
            AssetId = assetId,
            Title = "Service",
            CreatedAt = DateTimeOffset.UtcNow,
        };
        var checklistItem = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            MaintenanceItemId = maintenanceItem.Id,
            Text = "Change oil",
            Status = status,
            ResolvedAt = resolvedOn is { } d ? AsResolvedAt(d) : null,
            EngineId = engineId,
            TemplateStepId = templateStepId,
        };
        _dbContext.MaintenanceItems.Add(maintenanceItem);
        _dbContext.ChecklistItems.Add(checklistItem);
        return checklistItem;
    }

    [Fact]
    public async Task Never_Done_Pair_Has_Null_LastDoneAt_And_Is_Overdue_When_Interval_Configured()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();
        var (_, step) = SeedTemplateStep(intervalMonths: 12);
        SeedChecklistItem(assetId, step.Id, null, ChecklistItemStatus.Skipped, null);
        await _dbContext.SaveChangesAsync(ct);

        var entries = await _service.GetScheduleAsync(assetId, ct);

        var entry = Assert.Single(entries);
        Assert.Null(entry.LastDoneAt);
        Assert.Equal(MaintenanceDueStatus.Overdue, entry.DueStatus);
    }

    [Fact]
    public async Task Independent_Engines_Produce_Independent_Entries()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();
        var mainEngineId = Guid.NewGuid();
        var kickerEngineId = Guid.NewGuid();
        var (_, step) = SeedTemplateStep(engineScoped: true);

        var mainDone = new DateOnly(2026, 1, 1);
        var kickerDone = new DateOnly(2026, 3, 1);
        SeedChecklistItem(assetId, step.Id, mainEngineId, ChecklistItemStatus.Done, mainDone);
        SeedChecklistItem(assetId, step.Id, kickerEngineId, ChecklistItemStatus.Done, kickerDone);
        await _dbContext.SaveChangesAsync(ct);

        var entries = await _service.GetScheduleAsync(assetId, ct);

        Assert.Equal(2, entries.Count);
        var mainEntry = entries.Single(e => e.EngineId == mainEngineId);
        var kickerEntry = entries.Single(e => e.EngineId == kickerEngineId);
        Assert.Equal(AsResolvedAt(mainDone), mainEntry.LastDoneAt);
        Assert.Equal(AsResolvedAt(kickerDone), kickerEntry.LastDoneAt);
    }

    [Fact]
    public async Task Skipping_Does_Not_Advance_LastDoneAt()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();
        var (_, step) = SeedTemplateStep();

        var doneDate = new DateOnly(2026, 1, 1);
        SeedChecklistItem(assetId, step.Id, null, ChecklistItemStatus.Done, doneDate);
        SeedChecklistItem(assetId, step.Id, null, ChecklistItemStatus.Skipped, new DateOnly(2026, 6, 1));
        await _dbContext.SaveChangesAsync(ct);

        var entries = await _service.GetScheduleAsync(assetId, ct);

        var entry = Assert.Single(entries);
        Assert.Equal(AsResolvedAt(doneDate), entry.LastDoneAt);
    }

    [Fact]
    public async Task Nearest_Log_Entry_Is_Used_When_Present()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();
        var (_, step) = SeedTemplateStep();

        var doneDate = new DateOnly(2026, 6, 1);
        SeedChecklistItem(assetId, step.Id, null, ChecklistItemStatus.Done, doneDate);
        _dbContext.MileageLogs.Add(new MileageLog
        {
            Id = Guid.NewGuid(), AssetId = assetId, Date = new DateOnly(2026, 5, 30), OdometerReading = 340,
        });
        _dbContext.MileageLogs.Add(new MileageLog
        {
            Id = Guid.NewGuid(), AssetId = assetId, Date = new DateOnly(2026, 6, 10), OdometerReading = 500,
        });
        await _dbContext.SaveChangesAsync(ct);

        var entries = await _service.GetScheduleAsync(assetId, ct);

        var entry = Assert.Single(entries);
        Assert.NotNull(entry.LastDoneReading);
        Assert.Equal(340, entry.LastDoneReading!.Value);
        Assert.Equal(ReadingUnit.Miles, entry.LastDoneReading.Unit);
    }

    [Fact]
    public async Task No_Nearby_Log_Entry_Yields_Null_Reading()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();
        var (_, step) = SeedTemplateStep();

        var doneDate = new DateOnly(2026, 6, 1);
        SeedChecklistItem(assetId, step.Id, null, ChecklistItemStatus.Done, doneDate);
        await _dbContext.SaveChangesAsync(ct);

        var entries = await _service.GetScheduleAsync(assetId, ct);

        var entry = Assert.Single(entries);
        Assert.Null(entry.LastDoneReading);
        Assert.Equal(AsResolvedAt(doneDate), entry.LastDoneAt);
    }

    [Fact]
    public async Task Usage_Interval_Exceeded_Is_Overdue_Regardless_Of_Calendar()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();
        var engineId = Guid.NewGuid();
        var (_, step) = SeedTemplateStep(engineScoped: true, intervalHours: 100);

        var doneDate = new DateOnly(2026, 1, 1);
        SeedChecklistItem(assetId, step.Id, engineId, ChecklistItemStatus.Done, doneDate);
        _dbContext.EngineHoursLogs.Add(new EngineHoursLog
        {
            Id = Guid.NewGuid(), EngineId = engineId, Date = doneDate, HoursReading = 340,
        });
        _dbContext.EngineHoursLogs.Add(new EngineHoursLog
        {
            Id = Guid.NewGuid(), EngineId = engineId, Date = new DateOnly(2026, 6, 1), HoursReading = 450,
        });
        await _dbContext.SaveChangesAsync(ct);

        var entries = await _service.GetScheduleAsync(assetId, ct);

        var entry = Assert.Single(entries);
        Assert.Equal(MaintenanceDueStatus.Overdue, entry.DueStatus);
    }

    [Fact]
    public async Task Calendar_Only_Interval_Overdue_When_Past_Due_Date()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();
        var (_, step) = SeedTemplateStep(intervalMonths: 1);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var doneDate = today.AddMonths(-1).AddDays(-5);
        SeedChecklistItem(assetId, step.Id, null, ChecklistItemStatus.Done, doneDate);
        await _dbContext.SaveChangesAsync(ct);

        var entries = await _service.GetScheduleAsync(assetId, ct);

        Assert.Equal(MaintenanceDueStatus.Overdue, Assert.Single(entries).DueStatus);
    }

    [Fact]
    public async Task Calendar_Only_Interval_DueSoon_When_Within_Seven_Days()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();
        var (_, step) = SeedTemplateStep(intervalMonths: 1);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var doneDate = today.AddMonths(-1).AddDays(2);
        SeedChecklistItem(assetId, step.Id, null, ChecklistItemStatus.Done, doneDate);
        await _dbContext.SaveChangesAsync(ct);

        var entries = await _service.GetScheduleAsync(assetId, ct);

        Assert.Equal(MaintenanceDueStatus.DueSoon, Assert.Single(entries).DueStatus);
    }

    [Fact]
    public async Task Calendar_Only_Interval_Upcoming_When_Within_Thirty_Days()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();
        var (_, step) = SeedTemplateStep(intervalMonths: 1);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var doneDate = today.AddMonths(-1).AddDays(15);
        SeedChecklistItem(assetId, step.Id, null, ChecklistItemStatus.Done, doneDate);
        await _dbContext.SaveChangesAsync(ct);

        var entries = await _service.GetScheduleAsync(assetId, ct);

        Assert.Equal(MaintenanceDueStatus.Upcoming, Assert.Single(entries).DueStatus);
    }

    [Fact]
    public async Task Usage_Interval_With_No_Current_Reading_Is_Unknown()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();
        var engineId = Guid.NewGuid();
        var (_, step) = SeedTemplateStep(engineScoped: true, intervalHours: 100);

        SeedChecklistItem(assetId, step.Id, engineId, ChecklistItemStatus.Done, new DateOnly(2026, 1, 1));
        await _dbContext.SaveChangesAsync(ct);

        var entries = await _service.GetScheduleAsync(assetId, ct);

        Assert.Equal(MaintenanceDueStatus.Unknown, Assert.Single(entries).DueStatus);
    }
}
