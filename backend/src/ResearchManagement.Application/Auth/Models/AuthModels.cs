using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Application.Auth.Models;

public sealed class LoginRequest
{
    [Required, StringLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Password { get; set; } = string.Empty;
}

public sealed record RoleAssignmentDto(string Role, string Scope, long? FacultyId, long? DepartmentId);

public sealed record ProfileResponse(
    long Id,
    string Username,
    string FullName,
    string? Email,
    IReadOnlyList<RoleAssignmentDto> Roles);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    ProfileResponse Profile);

/// <summary>Tài khoản đang hoạt động, có vai trò giảng dạy/quản lý đơn vị và chưa gắn hồ sơ giảng viên.</summary>
public sealed record LecturerAccountOptionResponse(
    long Id,
    string Username,
    string FullName,
    string? Email,
    IReadOnlyList<string> Roles,
    long? FacultyId = null,
    long? DepartmentId = null);

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
