using Microsoft.AspNetCore.Authorization;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Api.Authorization;

public static class Policies
{
    /// <summary>Chỉ QUAN_TRI.</summary>
    public const string Admin = "Admin";

    /// <summary>Tên policy legacy; quyền hiện tại thuộc TRUONG_KHOA hoặc QUAN_TRI.</summary>
    public const string ResearchOffice = "ResearchOffice";

    /// <summary>Mọi vai trò quản lý hiện tại: TRUONG_BO_MON, TRUONG_KHOA, QUAN_TRI.</summary>
    public const string Management = "Management";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(Admin, p => p.RequireRole(Roles.Admin));
        options.AddPolicy(ResearchOffice, p => p.RequireRole(Roles.FacultyDean, Roles.Admin));
        options.AddPolicy(Management, p => p.RequireRole(
            Roles.DepartmentHead, Roles.FacultyDean, Roles.Admin));
    }
}
