using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Infrastructure.Auth;

namespace ResearchManagement.Api.Authorization;

public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private IReadOnlyList<RoleAssignmentDto>? _assignments;

    private System.Security.Claims.ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public long? Id => long.TryParse(Principal?.FindFirst(AppClaimTypes.Subject)?.Value, out var id) ? id : null;

    public IReadOnlyList<RoleAssignmentDto> Assignments => _assignments ??=
        Principal?.FindAll(AppClaimTypes.Assignment)
            .Select(c => AssignmentClaim.Parse(c.Value))
            .OfType<RoleAssignmentDto>()
            .ToList() ?? [];

    public bool IsInRole(string role) => Principal?.IsInRole(role) == true;

    public bool CanAccess(long? facultyId, long? departmentId) =>
        ScopeRules.CanAccess(Assignments, facultyId, departmentId);
}
