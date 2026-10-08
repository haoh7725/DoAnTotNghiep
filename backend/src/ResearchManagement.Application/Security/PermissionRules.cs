namespace ResearchManagement.Application.Security;
public record Permission(string Role, string Scope, long? FacultyId, long? DepartmentId);
public static class PermissionRules
{
    public static bool IsValid(Permission p) => p switch
    {
        { Role: "GIANG_VIEN", Scope: "CA_NHAN", FacultyId: null, DepartmentId: null } => true,
        { Role: "TRUONG_BO_MON", Scope: "BO_MON", FacultyId: > 0, DepartmentId: > 0 } => true,
        { Role: "TRUONG_KHOA", Scope: "KHOA", FacultyId: > 0, DepartmentId: null } => true,
        { Role: "PHONG_QLKH" or "BAN_GIAM_HIEU" or "QUAN_TRI", Scope: "TOAN_TRUONG", FacultyId: null, DepartmentId: null } => true,
        _ => false
    };
    public static bool CanRead(IEnumerable<Permission> permissions, long currentAccountId,
        long ownerAccountId, long facultyId, long departmentId) => permissions.Where(IsValid).Any(p =>
        p.Scope == "TOAN_TRUONG" ||
        p.Scope == "KHOA" && p.FacultyId == facultyId ||
        p.Scope == "BO_MON" && p.FacultyId == facultyId && p.DepartmentId == departmentId ||
        p.Scope == "CA_NHAN" && currentAccountId == ownerAccountId);
}
