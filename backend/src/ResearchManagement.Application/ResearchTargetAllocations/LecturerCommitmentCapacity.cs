using ResearchManagement.Application.Common;

namespace ResearchManagement.Application.ResearchTargetAllocations;

/// <summary>
/// Khả năng nhận thêm chỉ tiêu bài báo trong một năm học. Bài đã hoàn thành nhưng không gắn
/// với phân công chính thức vẫn sử dụng một phần cam kết cá nhân.
/// </summary>
public sealed record LecturerCommitmentCapacity(
    int CommittedArticles,
    int AssignedArticles,
    int CompletedOutsideAssignment)
{
    public int AvailableArticles => Math.Max(
        0,
        CommittedArticles - AssignedArticles - CompletedOutsideAssignment);

    public void EnsureCanAssign(int requestedArticles)
    {
        if (requestedArticles > AvailableArticles)
            throw new BusinessRuleException(
                $"Giảng viên chỉ còn có thể nhận {AvailableArticles} bài " +
                $"(cam kết {CommittedArticles}, đã giao {AssignedArticles}, " +
                $"hoàn thành ngoài phân công {CompletedOutsideAssignment}).");
    }
}
