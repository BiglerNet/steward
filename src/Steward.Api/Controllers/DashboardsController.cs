using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Authorization;
using Steward.Application.Dashboards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/dashboards")]
public class DashboardsController(
    IDashboardService dashboardService,
    IAuthorizationService authorizationService,
    IValidator<CreateDashboardRequest> createValidator,
    IValidator<UpdateDashboardRequest> updateValidator,
    IValidator<ReplaceWidgetLayoutRequest> layoutValidator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(Guid householdId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.View);
        if (!authResult.Succeeded) return Forbid();

        var dashboards = await dashboardService.ListAsync(householdId, cancellationToken);
        return Ok(dashboards);
    }

    [HttpGet("{dashboardId:guid}")]
    public async Task<IActionResult> Get(Guid householdId, Guid dashboardId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.View);
        if (!authResult.Succeeded) return Forbid();

        var dashboard = await dashboardService.GetAsync(householdId, dashboardId, cancellationToken);
        return Ok(dashboard);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        Guid householdId, CreateDashboardRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var dashboard = await dashboardService.CreateAsync(householdId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, dashboard);
    }

    [HttpPut("{dashboardId:guid}")]
    public async Task<IActionResult> Update(
        Guid householdId, Guid dashboardId, UpdateDashboardRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var dashboard = await dashboardService.UpdateAsync(householdId, dashboardId, request, cancellationToken);
        return Ok(dashboard);
    }

    [HttpDelete("{dashboardId:guid}")]
    public async Task<IActionResult> Delete(Guid householdId, Guid dashboardId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Delete);
        if (!authResult.Succeeded) return Forbid();

        await dashboardService.DeleteAsync(householdId, dashboardId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{dashboardId:guid}/widgets")]
    public async Task<IActionResult> ReplaceWidgetLayout(
        Guid householdId, Guid dashboardId, ReplaceWidgetLayoutRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.Edit);
        if (!authResult.Succeeded) return Forbid();

        var validation = await layoutValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return ValidationProblem(validation.ToModelState());

        var dashboard = await dashboardService.ReplaceWidgetLayoutAsync(householdId, dashboardId, request, cancellationToken);
        return Ok(dashboard);
    }

    [HttpGet("{dashboardId:guid}/snapshot")]
    public async Task<IActionResult> GetSnapshot(Guid householdId, Guid dashboardId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, HouseholdOperations.View);
        if (!authResult.Succeeded) return Forbid();

        var snapshot = await dashboardService.GetSnapshotAsync(householdId, dashboardId, cancellationToken);
        return Ok(snapshot);
    }

    private Task<AuthorizationResult> AuthorizeAsync(
        Guid householdId,
        Microsoft.AspNetCore.Authorization.Infrastructure.OperationAuthorizationRequirement operation)
    {
        return authorizationService.AuthorizeAsync(User, new HouseholdResource(householdId), operation);
    }
}
