namespace ResearchManagement.Application.Common;

/// <summary>Lỗi nghiệp vụ; lớp Api ánh xạ từng loại sang mã HTTP tương ứng.</summary>
public abstract class AppException(string message) : Exception(message);

/// <summary>Không tìm thấy dữ liệu (404).</summary>
public sealed class NotFoundException(string message) : AppException(message);

/// <summary>Dữ liệu xung đột, ví dụ trùng khóa duy nhất (409).</summary>
public sealed class ConflictException(string message) : AppException(message);

/// <summary>Vi phạm quy tắc nghiệp vụ (422).</summary>
public sealed class BusinessRuleException(string message) : AppException(message);

/// <summary>Xác thực thất bại (401).</summary>
public sealed class AuthenticationFailedException(string message) : AppException(message);

/// <summary>Đã xác thực nhưng không được phép thực hiện (403).</summary>
public sealed class ForbiddenException(string message) : AppException(message);
