using Steward.Domain.Entities;
using Steward.Domain.Entities.Assets;
using Steward.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Persistence;

public class StewardDbContext(DbContextOptions<StewardDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<Engine> Engines => Set<Engine>();
    public DbSet<ServiceRecord> ServiceRecords => Set<ServiceRecord>();
    public DbSet<MileageLog> MileageLogs => Set<MileageLog>();
    public DbSet<EngineHoursLog> EngineHoursLogs => Set<EngineHoursLog>();
    public DbSet<FuelLog> FuelLogs => Set<FuelLog>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseholdMembership> HouseholdMemberships => Set<HouseholdMembership>();
    public DbSet<HouseholdInvitation> HouseholdInvitations => Set<HouseholdInvitation>();
    public DbSet<HouseholdDashboard> HouseholdDashboards => Set<HouseholdDashboard>();
    public DbSet<DashboardWidget> DashboardWidgets => Set<DashboardWidget>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(StewardDbContext).Assembly);
    }
}
