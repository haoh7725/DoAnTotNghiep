using System.ComponentModel.DataAnnotations;
namespace ResearchManagement.Api.Contracts;
public sealed record FacultyRequest([Required, StringLength(30)] string Code, [Required, StringLength(200)] string Name);
public sealed record DepartmentRequest([Range(1, long.MaxValue)] long FacultyId, [Required, StringLength(30)] string Code, [Required, StringLength(200)] string Name);
public sealed record AcademicYearRequest([Required, StringLength(20)] string Code, DateOnly StartDate, DateOnly EndDate);
public sealed record CreateAccountRequest(
    [Required, StringLength(100), RegularExpression(@"[a-zA-Z0-9._-]+", ErrorMessage = "Tên đăng nhập chỉ gồm chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.")] string Username,
    [Required, StringLength(200)] string FullName,
    [EmailAddress, StringLength(254)] string? Email,
    [Required, StringLength(128, MinimumLength = 12)] string Password);
public sealed record UpdateAccountRequest([Required, StringLength(200)] string FullName,
    [EmailAddress, StringLength(254)] string? Email,
    [Required, RegularExpression("^(HOAT_DONG|KHOA)$")] string Status);
public sealed record PasswordRequest([Required, StringLength(128, MinimumLength = 12)] string Password);
public sealed record PermissionRequest([Required] string Role, [Required] string Scope, long? FacultyId, long? DepartmentId);
public sealed record ChangePasswordRequest([Required, StringLength(128)] string CurrentPassword,
    [Required, StringLength(128, MinimumLength = 12)] string NewPassword);
