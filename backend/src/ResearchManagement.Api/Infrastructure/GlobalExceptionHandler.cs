using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ResearchManagement.Application.Common;

namespace ResearchManagement.Api.Infrastructure;

/// <summary>Chuyển exception thành ProblemDetails (RFC 9457) với mã HTTP phù hợp.</summary>
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var postgres = exception as PostgresException ?? exception.InnerException as PostgresException;
        var (status, title) = postgres?.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => (StatusCodes.Status409Conflict, "Dữ liệu đã tồn tại"),
            PostgresErrorCodes.ForeignKeyViolation => (StatusCodes.Status409Conflict, "Dữ liệu đang được sử dụng"),
            PostgresErrorCodes.CheckViolation => (StatusCodes.Status422UnprocessableEntity, "Vi phạm quy tắc nghiệp vụ"),
            _ => exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Không tìm thấy"),
            ConflictException => (StatusCodes.Status409Conflict, "Xung đột dữ liệu"),
            BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "Vi phạm quy tắc nghiệp vụ"),
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Xác thực thất bại"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Không có quyền"),
            _ when exception is NpgsqlException or DbUpdateException =>
                (StatusCodes.Status503ServiceUnavailable, "Không thể xử lý dữ liệu"),
            _ => (StatusCodes.Status500InternalServerError, "Lỗi hệ thống")
        }
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Lỗi không xử lý được tại {Path}",
                httpContext.Request.Path);
        }

        var detail = exception switch
        {
            AppException => exception.Message,
            _ when postgresBusinessMessage is not null => postgresBusinessMessage,
            _ => "Đã xảy ra lỗi. Vui lòng thử lại sau."
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;

        await httpContext.Response.WriteAsJsonAsync(
            problem,
            cancellationToken);

        return true;
    }

    private static string? GetPostgresBusinessMessage(Exception exception)
    {
        if (exception is not DbUpdateException
            {
                InnerException: PostgresException postgresException
            })
        {
            return null;
        }

        return postgresException.MessageText switch
        {
            "Chi tieu moi nho hon tong da phan bo" =>
                "Chỉ tiêu mới không được nhỏ hơn tổng số bài đã phân bổ.",

            _ when postgresException.MessageText.StartsWith(
                "Tong phan bo vuot chi tieu duoc giao") =>
                "Tổng số bài phân bổ không được vượt quá chỉ tiêu được giao.",

            _ => null
        };
    }
}