using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng phan_quyen: một tài khoản có thể có nhiều vai trò, mỗi vai trò gắn với một phạm vi.</summary>
public sealed class RoleAssignment : BaseEntity
{
    private RoleAssignment() { }

    public RoleAssignment(string role, string scope, long? facultyId = null, long? departmentId = null)
    {
        Role = role;
        Scope = scope;
        FacultyId = facultyId;
        DepartmentId = departmentId;
    }

    public RoleAssignment(long accountId, string role, string scope, long? facultyId = null, long? departmentId = null)
        : this(role, scope, facultyId, departmentId) => AccountId = accountId;

    public long AccountId { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string Scope { get; private set; } = string.Empty;
    public long? FacultyId { get; private set; }
    public long? DepartmentId { get; private set; }
}
