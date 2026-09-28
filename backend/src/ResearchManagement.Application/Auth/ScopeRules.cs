using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Application.Auth;

public static class ScopeRules
{
    /// <summary>
    /// TOAN_TRUONG: mọi dữ liệu. KHOA: dữ liệu cùng khoa. BO_MON: dữ liệu cùng bộ môn.
    /// CA_NHAN không cấp quyền theo đơn vị (module tự kiểm tra chủ sở hữu).
    /// </summary>
    public static bool CanAccess(
        IEnumerable<RoleAssignmentDto> assignments, long? facultyId, long? departmentId)
    {
        foreach (var a in assignments)
        {
            switch (a.Scope)
            {
                case Scopes.Global:
                    return true;
                case Scopes.Faculty when facultyId.HasValue && a.FacultyId == facultyId:
                    return true;
                case Scopes.Department when departmentId.HasValue && a.DepartmentId == departmentId:
                    return true;
            }
        }
        return false;
    }
}
