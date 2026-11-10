using ResearchManagement.Application.Common;

namespace ResearchManagement.Application.ResearchTargetAllocations;

/// <summary>
/// Khả năng nhận thêm chỉ tiêu bài báo dựa trên cam kết cá nhân trong năm học.
/// </summary>
public sealed record LecturerCommitmentCapacity(
    int CommittedArticles,
    int AssignedArticles)
{
    public int AvailableArticles => Math.Max(
        0,
        CommittedArticles - AssignedArticles);

    public void EnsureCanAssign(int requestedArticles)
    {
        if (requestedArticles > AvailableArticles)
            throw new BusinessRuleException(
                $"Giảng viên chỉ còn có thể nhận {AvailableArticles} bài " +
                $"(cam kết {CommittedArticles}, đã giao {AssignedArticles}).");
    }
}
