using ResearchManagement.Application.Common;

namespace ResearchManagement.Application.Products;

public static class ArticleProgressPolicy
{
    public static int Rank(string? status) => status switch
    {
        null or "" or "DANG_VIET" => 0,
        "DANG_PHAN_BIEN" => 1,
        "DA_NHAN_DANG" or "DA_NHAN_XET" => 2,
        "DA_XUAT_BAN" => 3,
        _ => -1
    };

    public static void ValidateTransition(string current, string next, int evidenceCount)
    {
        var currentRank = Rank(current);
        var nextRank = Rank(next);
        if (currentRank < 0 || nextRank < 0) throw new BusinessRuleException("Trạng thái bài viết không hợp lệ.");
        if (nextRank < currentRank) throw new BusinessRuleException("Tiến độ bài viết không được lùi trạng thái.");
        if (nextRank > currentRank + 1) throw new BusinessRuleException("Bài viết phải đi qua từng bước theo thứ tự.");
        if (nextRank > currentRank && evidenceCount == 0)
            throw new BusinessRuleException("Phải có ít nhất một minh chứng trước khi chuyển bài viết sang bước tiếp theo.");
    }
}
