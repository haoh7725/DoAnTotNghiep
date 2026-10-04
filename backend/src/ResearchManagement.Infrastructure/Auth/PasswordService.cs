using Microsoft.AspNetCore.Identity;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Auth;

/// <summary>Băm mật khẩu bằng PBKDF2 của ASP.NET Core Identity (có salt, tự nâng cấp hash khi đổi thuật toán).</summary>
public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<Account> _hasher = new();

    public string Hash(Account account, string password) => _hasher.HashPassword(account, password);

    public (bool Succeeded, bool NeedsRehash) Verify(Account account, string password)
    {
        var result = _hasher.VerifyHashedPassword(account, account.PasswordHash, password);
        return result switch
        {
            PasswordVerificationResult.Success => (true, false),
            PasswordVerificationResult.SuccessRehashNeeded => (true, true),
            _ => (false, false)
        };
    }
}
