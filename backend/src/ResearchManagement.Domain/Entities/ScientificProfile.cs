using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

/// <summary>Bảng ly_lich_khoa_hoc. Mỗi giảng viên có tối đa một lý lịch khoa học.</summary>
public sealed class ScientificProfile : BaseEntity
{
    private ScientificProfile() { }

    public ScientificProfile(
        long lecturerId, string? expertise, string? researchFields,
        string? researchDirections, string? activitySummary, DateTimeOffset now)
    {
        LecturerId = lecturerId;
        Update(expertise, researchFields, researchDirections, activitySummary, now);
    }

    public long LecturerId { get; private set; }
    public string? Expertise { get; private set; }
    public string? ResearchFields { get; private set; }
    public string? ResearchDirections { get; private set; }
    public string? ActivitySummary { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(
        string? expertise, string? researchFields,
        string? researchDirections, string? activitySummary, DateTimeOffset now)
    {
        Expertise = expertise;
        ResearchFields = researchFields;
        ResearchDirections = researchDirections;
        ActivitySummary = activitySummary;
        UpdatedAt = now;
    }
}
