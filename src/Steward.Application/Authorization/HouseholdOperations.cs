using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Steward.Application.Authorization;

public static class HouseholdOperations
{
    public static readonly OperationAuthorizationRequirement View = new() { Name = nameof(View) };
    public static readonly OperationAuthorizationRequirement Edit = new() { Name = nameof(Edit) };
    public static readonly OperationAuthorizationRequirement Delete = new() { Name = nameof(Delete) };
    public static readonly OperationAuthorizationRequirement Invite = new() { Name = nameof(Invite) };
}
