using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace ResearchManagement.Api.Middleware;
public sealed class ApiErrors(ILogger<ApiErrors> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        var pg = exception as PostgresException ?? exception.InnerException as PostgresException;
        var (status, title) = pg?.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => (409, "Mã, tên đăng nhập, email hoặc quyền đã tồn tại."),
            PostgresErrorCodes.ForeignKeyViolation => (409, "Dữ liệu tham chiếu không tồn tại hoặc bản ghi đang được sử dụng."),
            PostgresErrorCodes.CheckViolation => (400, "Dữ liệu không thỏa mãn ràng buộc nghiệp vụ."),
            _ when exception is NpgsqlException || exception.InnerException is NpgsqlException => (503, "Không thể kết nối hoặc xử lý dữ liệu. Vui lòng thử lại."),
            _ => (500, "Đã xảy ra lỗi máy chủ.")
        };
        if (status >= 500) logger.LogError(exception, "API request failed: {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails { Status = status, Title = title,
            Extensions = { ["traceId"] = context.TraceIdentifier } }, ct);
        return true;
    }
}
