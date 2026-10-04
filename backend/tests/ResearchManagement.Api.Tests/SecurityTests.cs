using ResearchManagement.Application.Security;

namespace ResearchManagement.Api.Tests;
public class SecurityTests
{
    [Theory]
    [InlineData("GIANG_VIEN", "CA_NHAN", null, null, true)]
    [InlineData("GIANG_VIEN", "TOAN_TRUONG", null, null, false)]
    [InlineData("QUAN_TRI", "TOAN_TRUONG", null, null, true)]
    [InlineData("QUAN_TRI", "KHOA", 1L, null, false)]
    [InlineData("TRUONG_KHOA", "KHOA", 1L, null, true)]
    [InlineData("TRUONG_KHOA", "KHOA", 1L, 2L, false)]
    [InlineData("TRUONG_BO_MON", "BO_MON", 1L, 2L, true)]
    [InlineData("TRUONG_BO_MON", "BO_MON", null, 2L, false)]
    [InlineData("PHONG_QLKH", "TOAN_TRUONG", null, null, true)]
    [InlineData("BAN_GIAM_HIEU", "TOAN_TRUONG", null, null, true)]
    [InlineData("UNKNOWN", "TOAN_TRUONG", null, null, false)]
    public void ValidatesRoleAndScopeTogether(string role, string scope, long? faculty, long? department, bool expected)
        => Assert.Equal(expected, PermissionRules.IsValid(new(role, scope, faculty, department)));

    [Fact]
    public void PersonalScopeCannotReadAnotherOwner()
    {
        Permission[] permissions = [new("GIANG_VIEN", "CA_NHAN", null, null)];
        Assert.True(PermissionRules.CanRead(permissions, 5, 5, 1, 2));
        Assert.False(PermissionRules.CanRead(permissions, 5, 6, 1, 2));
    }
    [Fact]
    public void DepartmentScopeCannotEscapeItsFacultyOrDepartment()
    {
        Permission[] permissions = [new("TRUONG_BO_MON", "BO_MON", 1, 2)];
        Assert.True(PermissionRules.CanRead(permissions, 5, 6, 1, 2));
        Assert.False(PermissionRules.CanRead(permissions, 5, 6, 1, 3));
        Assert.False(PermissionRules.CanRead(permissions, 5, 6, 9, 2));
    }
    [Fact]
    public void FacultyScopeAndMultipleAssignmentsUseUnionOfValidScopes()
    {
        Permission[] permissions = [new("TRUONG_KHOA", "KHOA", 1, null), new("TRUONG_BO_MON", "BO_MON", 2, 3)];
        Assert.True(PermissionRules.CanRead(permissions, 5, 6, 1, 9));
        Assert.True(PermissionRules.CanRead(permissions, 5, 6, 2, 3));
        Assert.False(PermissionRules.CanRead(permissions, 5, 6, 2, 4));
        Assert.False(PermissionRules.CanRead([], 5, 5, 1, 2));
        Assert.False(PermissionRules.CanRead([new("UNKNOWN", "TOAN_TRUONG", null, null)], 5, 6, 1, 2));
    }
}
