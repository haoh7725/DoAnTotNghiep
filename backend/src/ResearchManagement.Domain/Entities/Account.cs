using ResearchManagement.Domain.Common;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng tai_khoan.</summary>
public sealed class Account : BaseEntity
{
    private readonly List<RoleAssignment> _roleAssignments = [];

    private Account() { }

    public Account(string username, string passwordHash, string fullName, string? email = null)
    {
        Username = username;
        PasswordHash = passwordHash;
        FullName = fullName;
        Email = email;
        Status = AccountStatuses.Active;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string Status { get; private set; } = AccountStatuses.Active;
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyCollection<RoleAssignment> RoleAssignments => _roleAssignments;

    public bool IsActive => Status == AccountStatuses.Active;

    public void AddRole(RoleAssignment assignment) => _roleAssignments.Add(assignment);
    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;
    public void Lock() => Status = AccountStatuses.Locked;
    public void Unlock() => Status = AccountStatuses.Active;
}
