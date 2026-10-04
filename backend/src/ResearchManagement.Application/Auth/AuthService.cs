using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Application.Common;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Auth;

public sealed class AuthService(
    IAccountRepository accounts,
    IPasswordService passwords,
    ITokenService tokens)
{
    private const string InvalidCredentials = "Tên đăng nhập hoặc mật khẩu không đúng.";

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByUsernameAsync(request.Username.Trim(), cancellationToken)
            ?? throw new AuthenticationFailedException(InvalidCredentials);

        var (succeeded, needsRehash) = passwords.Verify(account, request.Password);
        if (!succeeded)
            throw new AuthenticationFailedException(InvalidCredentials);

        // Chỉ báo tài khoản bị khóa sau khi mật khẩu đúng, tránh lộ thông tin tài khoản.
        if (!account.IsActive)
            throw new ForbiddenException("Tài khoản đã bị khóa.");

        if (needsRehash)
        {
            account.SetPasswordHash(passwords.Hash(account, request.Password));
            await accounts.SaveChangesAsync(cancellationToken);
        }

        var token = tokens.CreateAccessToken(account);
        return new LoginResponse(token.Value, "Bearer", token.ExpiresAt, ToProfile(account));
    }

    public async Task<ProfileResponse> GetProfileAsync(long accountId, CancellationToken cancellationToken)
    {
        var account = await accounts.GetByIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy tài khoản.");
        return ToProfile(account);
    }

    private static ProfileResponse ToProfile(Account account) => new(
        account.Id,
        account.Username,
        account.FullName,
        account.Email,
        account.RoleAssignments
            .Select(r => new RoleAssignmentDto(r.Role, r.Scope, r.FacultyId, r.DepartmentId))
            .ToList());
}
