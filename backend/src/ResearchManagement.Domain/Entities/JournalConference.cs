
namespace ResearchManagement.Domain.Entities;

public sealed class JournalConference
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    // TAP_CHI hoặc HOI_NGHI
    public string Type { get; set; } = string.Empty;

    public string? Issn { get; set; }

    public string? Index { get; set; }

    public string? Classification { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
