using ResearchManagement.Application.Common;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Application.Products;

public static class ArticleProgressRequestPolicy
{
    public static string Next(string? current) => current switch
    {
        null or "" or "DANG_VIET" => "DANG_PHAN_BIEN",
        "DANG_PHAN_BIEN" => "DA_NHAN_XET",
        "DA_NHAN_DANG" or "DA_NHAN_XET" => "DA_XUAT_BAN",
        "DA_XUAT_BAN" => throw new BusinessRuleException("Bài báo đã ở bước xuất bản cuối cùng."),
        _ => throw new BusinessRuleException("Trạng thái bài báo hiện tại không hợp lệ.")
    };

    public static string RequiredRole(string next) => next switch
    {
        "DANG_PHAN_BIEN" => Roles.DepartmentHead,
        "DA_NHAN_XET" or "DA_XUAT_BAN" => Roles.FacultyDean,
        _ => throw new BusinessRuleException("Trạng thái đề nghị không hợp lệ.")
    };
}
