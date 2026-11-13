using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng chi_tieu_nckh.</summary>
public sealed class ResearchTarget : BaseEntity
{
    private ResearchTarget() { }

    public ResearchTarget(
        long facultyId,
        long academicYearId,
        int assignedArticleCount,
        DateOnly? deadline,
        long? journalConferenceId = null,
        long? productTypeId = null)
    {
        if (assignedArticleCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assignedArticleCount),
                "Số bài được giao phải lớn hơn 0.");
        }

        FacultyId = facultyId;
        AcademicYearId = academicYearId;
        AssignedArticleCount = assignedArticleCount;
        Deadline = deadline;
        JournalConferenceId = journalConferenceId;
        ProductTypeId = productTypeId;
        Status = "CHO_TIEP_NHAN";
    }

    public long FacultyId { get; private set; }

    public long AcademicYearId { get; private set; }

    public int AssignedArticleCount { get; private set; }

    public DateOnly? Deadline { get; private set; }
    public long? JournalConferenceId { get; private set; }
    public long? ProductTypeId { get; private set; }
    public string Status { get; private set; } = "CHO_TIEP_NHAN";
    public long? AssignedByAccountId { get; private set; }
    public long? AcceptedByAccountId { get; private set; }
    public DateTimeOffset? AcceptedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? LockedAt { get; private set; }

    public void Update(
        int assignedArticleCount,
        DateOnly? deadline,
        long? journalConferenceId = null,
        long? productTypeId = null)
    {
        if (assignedArticleCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(assignedArticleCount),
                "Số bài được giao phải lớn hơn 0.");
        }

        AssignedArticleCount = assignedArticleCount;
        Deadline = deadline;
        JournalConferenceId = journalConferenceId;
        if (productTypeId.HasValue) ProductTypeId = productTypeId;
    }

    public void MarkAssigned(long accountId) => AssignedByAccountId = accountId;

    public void ExtendDeadline(DateOnly date)
    {
        if (Status == "DA_HUY") throw new InvalidOperationException("Không gia hạn chỉ tiêu đã hủy.");
        if (Deadline.HasValue && date <= Deadline.Value)
            throw new InvalidOperationException("Hạn mới phải sau hạn hiện tại.");
        Deadline = date;
    }

    public void Accept(long accountId, DateTimeOffset now)
    {
        if (Status != "CHO_TIEP_NHAN") throw new InvalidOperationException("Chỉ tiêu không ở trạng thái chờ tiếp nhận.");
        AcceptedByAccountId = accountId;
        AcceptedAt = now;
        Status = "DA_TIEP_NHAN";
    }

    public void Publish(DateTimeOffset now)
    {
        if (Status != "DA_TIEP_NHAN") throw new InvalidOperationException("Phải tiếp nhận chỉ tiêu trước khi công bố.");
        PublishedAt = now;
        Status = "CONG_BO";
    }

    public void Lock(DateTimeOffset now)
    {
        if (Status != "CONG_BO") throw new InvalidOperationException("Chỉ chỉ tiêu đã công bố mới được khóa.");
        LockedAt = now;
        Status = "DA_KHOA";
    }

    public void Cancel()
    {
        if (Status == "DA_KHOA") throw new InvalidOperationException("Chỉ tiêu đã khóa không thể hủy.");
        Status = "DA_HUY";
    }
}
