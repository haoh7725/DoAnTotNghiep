using Microsoft.AspNetCore.Authorization;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Api.Authorization;

public static class Policies
{
    /// <summary>Chỉ QUAN_TRI.</summary>
    public const string Admin = "Admin";

    /// <summary>PHONG_QLKH hoặc QUAN_TRI.</summary>
    public const string ResearchOffice = "ResearchOffice";

    /// <summary>Mọi vai trò quản lý: TRUONG_BO_MON, TRUONG_KHOA, PHONG_QLKH, BAN_GIAM_HIEU, QUAN_TRI.</summary>
    public const string Management = "Management";

    public static void Register(AuthorizationOptions options)
    {
        options.AddPolicy(Admin, p => p.RequireRole(Roles.Admin));
        options.AddPolicy(ResearchOffice, p => p.RequireRole(Roles.ResearchOffice, Roles.Admin));
        options.AddPolicy(Management, p => p.RequireRole(
            Roles.DepartmentHead, Roles.FacultyDean, Roles.ResearchOffice, Roles.BoardOfDirectors, Roles.Admin));
    }
}
