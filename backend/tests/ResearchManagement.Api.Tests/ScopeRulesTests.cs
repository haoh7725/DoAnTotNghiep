using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Api.Tests;

public class ScopeRulesTests
{
    [Fact]
    public void GlobalScope_CanAccessEverything()
    {
        var a = new[] { new RoleAssignmentDto(Roles.Admin, Scopes.Global, null, null) };
        Assert.True(ScopeRules.CanAccess(a, 1, 2));
        Assert.True(ScopeRules.CanAccess(a, null, null));
    }

    [Fact]
    public void FacultyScope_OnlyOwnFaculty()
    {
        var a = new[] { new RoleAssignmentDto(Roles.FacultyDean, Scopes.Faculty, 5, null) };
        Assert.True(ScopeRules.CanAccess(a, 5, 99));
        Assert.False(ScopeRules.CanAccess(a, 6, 99));
        Assert.False(ScopeRules.CanAccess(a, null, 99));
    }

    [Fact]
    public void DepartmentScope_OnlyOwnDepartment()
    {
        var a = new[] { new RoleAssignmentDto(Roles.DepartmentHead, Scopes.Department, 5, 7) };
        Assert.True(ScopeRules.CanAccess(a, 5, 7));
        Assert.False(ScopeRules.CanAccess(a, 5, 8));
    }

    [Fact]
    public void PersonalScope_GrantsNoUnitAccess()
    {
        var a = new[] { new RoleAssignmentDto(Roles.Lecturer, Scopes.Personal, null, null) };
        Assert.False(ScopeRules.CanAccess(a, 5, 7));
    }

    [Fact]
    public void MultipleAssignments_AnyMatchWins()
    {
        var a = new[]
        {
            new RoleAssignmentDto(Roles.Lecturer, Scopes.Personal, null, null),
            new RoleAssignmentDto(Roles.DepartmentHead, Scopes.Department, 5, 7)
        };
        Assert.True(ScopeRules.CanAccess(a, 5, 7));
    }
}
