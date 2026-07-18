using Steward.Application.Common;
using Steward.Application.Tracking.MaintenanceItems;
using Steward.Domain.Enums;
using Steward.Infrastructure.Maintenance;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Steward.UnitTests.Maintenance;

public class MaintenanceItemServiceTests
{
    private readonly StewardDbContext _dbContext;
    private readonly MaintenanceItemService _service;

    public MaintenanceItemServiceTests()
    {
        // MaintenanceItemService.CreateAsync opens a transaction unconditionally (needed for the
        // real Postgres provider's template-expansion path); the InMemory provider doesn't support
        // transactions and warns by default, so that warning is suppressed here rather than in
        // production code.
        var dbOptions = new DbContextOptionsBuilder<StewardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new StewardDbContext(dbOptions);
        _service = new MaintenanceItemService(_dbContext);
    }

    [Fact]
    public async Task IsBlocked_True_When_A_Part_Line_Is_Needed_Or_Ordered()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();

        var created = await _service.CreateAsync(
            assetId, new CreateMaintenanceItemRequest("Brake job", null, null, null, null, null, null, null, null, null), ct);

        await _service.CreatePartLineAsync(
            assetId, created.Id, new CreatePartLineRequest("Brake pads", null, null, null, null, null, null, null), ct);

        var fetched = await _service.GetAsync(assetId, created.Id, ct);

        Assert.True(fetched.IsBlocked);
    }

    [Fact]
    public async Task IsBlocked_False_When_All_Part_Lines_Received()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();

        var created = await _service.CreateAsync(
            assetId, new CreateMaintenanceItemRequest("Brake job", null, null, null, null, null, null, null, null, null), ct);

        var partLine = await _service.CreatePartLineAsync(
            assetId, created.Id, new CreatePartLineRequest("Brake pads", null, null, null, null, null, null, null), ct);

        await _service.PatchPartLineAsync(
            assetId,
            created.Id,
            partLine.Id,
            new PatchPartLineRequest(
                Optional<string>.Unset,
                Optional<string?>.Unset,
                Optional<string?>.Unset,
                Optional<string?>.Unset,
                Optional<string?>.Unset,
                Optional<decimal>.Unset,
                Optional<PartLineStatus>.Of(PartLineStatus.Received),
                Optional<decimal?>.Unset,
                Optional<Guid?>.Unset),
            ct);

        var fetched = await _service.GetAsync(assetId, created.Id, ct);

        Assert.False(fetched.IsBlocked);
    }

    [Fact]
    public async Task IsBlocked_False_With_No_Part_Lines()
    {
        var ct = TestContext.Current.CancellationToken;
        var assetId = Guid.NewGuid();

        var created = await _service.CreateAsync(
            assetId,
            new CreateMaintenanceItemRequest("Oil change", null, null, MaintenanceItemStatus.Done, null, null, null, null, null, null),
            ct);

        Assert.False(created.IsBlocked);
    }
}
