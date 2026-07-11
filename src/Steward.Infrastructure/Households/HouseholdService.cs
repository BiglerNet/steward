using Steward.Application.Households;
using Steward.Application.Households.Memberships;
using Steward.Application.Storage;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Steward.Infrastructure.Households;

public class HouseholdService(StewardDbContext dbContext, IOptions<FileUploadOptions> uploadOptions) : IHouseholdService
{
    public async Task<HouseholdResponse> CreateAsync(
        Guid userId, CreateHouseholdRequest request, CancellationToken cancellationToken = default)
    {
        if (await dbContext.Households.AnyAsync(h => h.PublicSlug == request.PublicSlug, cancellationToken))
        {
            throw new ConflictException($"Slug '{request.PublicSlug}' is already in use.");
        }

        var household = new Household
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            PublicSlug = request.PublicSlug,
            IsPublicVisible = request.IsPublicVisible,
            Country = request.Country,
            Region = request.Region,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = userId,
        };

        var membership = new HouseholdMembership
        {
            Id = Guid.NewGuid(),
            HouseholdId = household.Id,
            UserId = userId,
            Role = HouseholdMemberRole.Owner,
            Status = HouseholdMemberStatus.Active,
            InvitedAt = DateTimeOffset.UtcNow,
            AcceptedAt = DateTimeOffset.UtcNow,
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Households.Add(household);
        dbContext.HouseholdMemberships.Add(membership);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException($"Slug '{request.PublicSlug}' is already in use.");
        }

        await transaction.CommitAsync(cancellationToken);

        return ToResponse(household, HouseholdMemberRole.Owner);
    }

    public async Task<HouseholdResponse> GetByIdAsync(
        Guid userId, Guid householdId, CancellationToken cancellationToken = default)
    {
        var household = await dbContext.Households.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken)
            ?? throw new BadRequestException("Household not found.");

        var membership = await dbContext.HouseholdMemberships.AsNoTracking()
            .FirstOrDefaultAsync(
                m => m.HouseholdId == householdId && m.UserId == userId && m.Status == HouseholdMemberStatus.Active,
                cancellationToken);

        return ToResponse(household, membership?.Role);
    }

    public async Task<IReadOnlyCollection<HouseholdResponse>> ListForUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        var defaultQuota = uploadOptions.Value.HouseholdQuotaBytes;

        return await dbContext.HouseholdMemberships.AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == HouseholdMemberStatus.Active)
            .Join(dbContext.Households, m => m.HouseholdId, h => h.Id, (m, h) => new HouseholdResponse(
                h.Id,
                h.Name,
                h.PublicSlug,
                h.IsPublicVisible,
                h.Country,
                h.Region,
                m.Role.ToString(),
                h.StorageUsedBytes,
                h.StorageQuotaOverrideBytes ?? defaultQuota,
                h.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<HouseholdResponse> UpdateAsync(
        Guid householdId, UpdateHouseholdRequest request, CancellationToken cancellationToken = default)
    {
        var household = await dbContext.Households
            .FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken)
            ?? throw new BadRequestException("Household not found.");

        if (household.PublicSlug != request.PublicSlug &&
            await dbContext.Households.AnyAsync(h => h.PublicSlug == request.PublicSlug, cancellationToken))
        {
            throw new ConflictException($"Slug '{request.PublicSlug}' is already in use.");
        }

        household.Name = request.Name;
        household.PublicSlug = request.PublicSlug;
        household.IsPublicVisible = request.IsPublicVisible;
        household.Country = request.Country;
        household.Region = request.Region;

        await dbContext.SaveChangesAsync(cancellationToken);

        var membership = await dbContext.HouseholdMemberships.AsNoTracking()
            .FirstOrDefaultAsync(m => m.HouseholdId == householdId && m.Role == HouseholdMemberRole.Owner, cancellationToken);

        return ToResponse(household, membership?.Role);
    }

    public async Task DeleteAsync(Guid householdId, CancellationToken cancellationToken = default)
    {
        var household = await dbContext.Households
            .FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken)
            ?? throw new BadRequestException("Household not found.");

        if (await dbContext.Assets.AnyAsync(a => a.HouseholdId == householdId, cancellationToken))
        {
            throw new ConflictException("Household has associated assets and cannot be deleted.");
        }

        dbContext.Households.Remove(household);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<InvitationResponse> InviteMemberAsync(
        Guid householdId, Guid invitedByUserId, InviteMemberRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var hasPending = await dbContext.HouseholdInvitations.AnyAsync(
            i => i.HouseholdId == householdId &&
                 i.Email == request.Email &&
                 i.Status == InvitationStatus.Pending &&
                 i.ExpiresAt > now,
            cancellationToken);

        if (hasPending)
        {
            throw new ConflictException("A pending invitation already exists for this email.");
        }

        var invitation = new HouseholdInvitation
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            InvitedByUserId = invitedByUserId,
            Email = request.Email,
            Role = request.Role,
            InviteCode = Guid.NewGuid().ToString("N"),
            ExpiresAt = now.AddDays(7),
            Status = InvitationStatus.Pending,
            CreatedAt = now,
        };

        dbContext.HouseholdInvitations.Add(invitation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToResponse(invitation);
    }

    public async Task AcceptInviteAsync(Guid userId, string inviteCode, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var invitation = await dbContext.HouseholdInvitations.FirstOrDefaultAsync(
            i => i.InviteCode == inviteCode && i.Status == InvitationStatus.Pending && i.ExpiresAt > now,
            cancellationToken)
            ?? throw new BadRequestException("Invitation code is invalid or expired.");

        var alreadyMember = await dbContext.HouseholdMemberships.AnyAsync(
            m => m.HouseholdId == invitation.HouseholdId && m.UserId == userId && m.Status == HouseholdMemberStatus.Active,
            cancellationToken);

        if (alreadyMember)
        {
            throw new ConflictException("You are already a member of this household.");
        }

        dbContext.HouseholdMemberships.Add(new HouseholdMembership
        {
            Id = Guid.NewGuid(),
            HouseholdId = invitation.HouseholdId,
            UserId = userId,
            Role = invitation.Role,
            Status = HouseholdMemberStatus.Active,
            InvitedByUserId = invitation.InvitedByUserId,
            InvitedAt = invitation.CreatedAt,
            AcceptedAt = now,
        });

        invitation.Status = InvitationStatus.Accepted;
        invitation.AcceptedByUserId = userId;
        invitation.AcceptedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeInviteAsync(Guid householdId, string inviteCode, CancellationToken cancellationToken = default)
    {
        var invitation = await dbContext.HouseholdInvitations.FirstOrDefaultAsync(
            i => i.HouseholdId == householdId && i.InviteCode == inviteCode, cancellationToken)
            ?? throw new BadRequestException("Invitation not found.");

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new BadRequestException("Only pending invitations can be revoked.");
        }

        invitation.Status = InvitationStatus.Revoked;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeMemberAsync(
        Guid householdId, Guid actingUserId, Guid targetUserId, CancellationToken cancellationToken = default)
    {
        if (actingUserId == targetUserId)
        {
            throw new BadRequestException("Owners cannot revoke their own membership.");
        }

        var membership = await dbContext.HouseholdMemberships.FirstOrDefaultAsync(
            m => m.HouseholdId == householdId && m.UserId == targetUserId && m.Status == HouseholdMemberStatus.Active,
            cancellationToken)
            ?? throw new BadRequestException("Membership not found.");

        membership.Status = HouseholdMemberStatus.Revoked;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<HouseholdMembersResponse> ListMembersAsync(
        Guid householdId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var members = await dbContext.HouseholdMemberships.AsNoTracking()
            .Where(m => m.HouseholdId == householdId && m.Status == HouseholdMemberStatus.Active)
            .Join(dbContext.Users, m => m.UserId, u => u.Id, (m, u) => new MembershipResponse(
                u.Id, u.DisplayName, u.Email!, m.Role, m.Status))
            .ToListAsync(cancellationToken);

        var pendingInvites = await dbContext.HouseholdInvitations.AsNoTracking()
            .Where(i => i.HouseholdId == householdId && i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .Select(i => new InvitationResponse(i.Id, i.Email, i.Role, i.InviteCode, i.ExpiresAt, i.Status))
            .ToListAsync(cancellationToken);

        return new HouseholdMembersResponse(members, pendingInvites);
    }

    public async Task SetStorageQuotaOverrideAsync(
        Guid householdId, long? quotaBytes, CancellationToken cancellationToken = default)
    {
        if (quotaBytes is <= 0)
        {
            throw new BadRequestException("quotaBytes must be positive.");
        }

        var household = await dbContext.Households
            .FirstOrDefaultAsync(h => h.Id == householdId, cancellationToken)
            ?? throw new BadRequestException("Household not found.");

        household.StorageQuotaOverrideBytes = quotaBytes;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private HouseholdResponse ToResponse(Household household, HouseholdMemberRole? role) => new(
        household.Id,
        household.Name,
        household.PublicSlug,
        household.IsPublicVisible,
        household.Country,
        household.Region,
        role?.ToString() ?? "PlatformAdmin",
        household.StorageUsedBytes,
        household.StorageQuotaOverrideBytes ?? uploadOptions.Value.HouseholdQuotaBytes,
        household.CreatedAt);

    private static InvitationResponse ToResponse(HouseholdInvitation invitation) => new(
        invitation.Id, invitation.Email, invitation.Role, invitation.InviteCode, invitation.ExpiresAt, invitation.Status);
}
