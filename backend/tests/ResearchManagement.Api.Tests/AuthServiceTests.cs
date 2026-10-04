using Microsoft.IdentityModel.JsonWebTokens;
using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Application.Common;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Auth;

namespace ResearchManagement.Api.Tests;

public class AuthServiceTests
{
    private static readonly JwtOptions Jwt = new()
    {
        Issuer = "test",
        Audience = "test-clients",
        SecretKey = "unit-test-secret-key-0123456789-abcdefghij",
        ExpiryMinutes = 30
    };

    private readonly PasswordService _passwords = new();
    private readonly FakeAccountRepository _repo = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(_repo, _passwords, new JwtTokenService(Jwt, TimeProvider.System));
    }

    private Account AddAccount(string username, string password, bool locked = false)
    {
        var account = new Account(username, string.Empty, "Nguyễn Văn A");
        account.SetPasswordHash(_passwords.Hash(account, password));
        account.AddRole(new RoleAssignment(Roles.DepartmentHead, Scopes.Department, 1, 2));
        account.AddRole(new RoleAssignment(Roles.Lecturer, Scopes.Personal));
        if (locked) account.Lock();
        _repo.Items.Add(account);
        return account;
    }

    [Fact]
    public async Task Login_Succeeds_AndTokenCarriesRolesAndAssignments()
    {
        AddAccount("hao", "Passw0rd!");

        var result = await _sut.LoginAsync(new LoginRequest { Username = "hao", Password = "Passw0rd!" }, default);

        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(2, result.Profile.Roles.Count);

        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(result.AccessToken);
        var roles = jwt.Claims.Where(c => c.Type == AppClaimTypes.Role).Select(c => c.Value).ToList();
        Assert.Contains(Roles.DepartmentHead, roles);
        Assert.Contains(Roles.Lecturer, roles);
        Assert.Contains(jwt.Claims, c => c.Type == AppClaimTypes.Assignment && c.Value == "TRUONG_BO_MON|BO_MON|1|2");
        Assert.Contains(jwt.Claims, c => c.Type == AppClaimTypes.Assignment && c.Value == "GIANG_VIEN|CA_NHAN||");
    }

    [Fact]
    public async Task Login_WrongPassword_And_UnknownUser_GiveSameError()
    {
        AddAccount("hao", "Passw0rd!");

        var wrong = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.LoginAsync(new LoginRequest { Username = "hao", Password = "sai" }, default));
        var unknown = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.LoginAsync(new LoginRequest { Username = "khong-ton-tai", Password = "x" }, default));

        Assert.Equal(wrong.Message, unknown.Message);
    }

    [Fact]
    public async Task Login_LockedAccount_WithCorrectPassword_IsForbidden()
    {
        AddAccount("khoa", "Passw0rd!", locked: true);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.LoginAsync(new LoginRequest { Username = "khoa", Password = "Passw0rd!" }, default));
    }

    [Fact]
    public async Task Login_LockedAccount_WithWrongPassword_DoesNotRevealLock()
    {
        AddAccount("khoa", "Passw0rd!", locked: true);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            _sut.LoginAsync(new LoginRequest { Username = "khoa", Password = "sai" }, default));
    }

    private sealed class FakeAccountRepository : IAccountRepository
    {
        public List<Account> Items { get; } = [];

        public Task<Account?> GetByUsernameAsync(string username, CancellationToken ct) =>
            Task.FromResult(Items.FirstOrDefault(a => a.Username == username));

        public Task<Account?> GetByIdAsync(long id, CancellationToken ct) =>
            Task.FromResult(Items.FirstOrDefault(a => a.Id == id));

        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken ct) =>
            Task.FromResult(Items.Any(a => a.Username == username));

        public Task AddAsync(Account account, CancellationToken ct)
        {
            Items.Add(account);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
