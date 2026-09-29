using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Auth.Abstractions;

public interface IAccountRepository
{
    Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken);
    Task<Account?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IPasswordService
{
    string Hash(Account account, string password);

    /// <summary>Kiểm tra mật khẩu. NeedsRehash = true khi hash cũ nên được băm lại.</summary>
    (bool Succeeded, bool NeedsRehash) Verify(Account account, string password);
}

public interface ITokenService
{
    AccessToken CreateAccessToken(Account account);
}

/// <summary>Người dùng hiện tại lấy từ JWT của request.</summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    long? Id { get; }
    IReadOnlyList<RoleAssignmentDto> Assignments { get; }
    bool IsInRole(string role);

    /// <summary>Có được truy cập dữ liệu thuộc khoa/bộ môn này hay không (theo phạm vi phân quyền).</summary>
    bool CanAccess(long? facultyId, long? departmentId);
}
