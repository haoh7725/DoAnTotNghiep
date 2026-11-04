using ResearchManagement.Domain.Common;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Domain.Entities;

public sealed class PlagiarismCheck : BaseEntity
{
    private PlagiarismCheck() { }

    public PlagiarismCheck(long productId, long? evidenceId, long requestedByAccountId,
        string fileHash, bool thirdPartyConsent)
    {
        ProductId = productId;
        EvidenceId = evidenceId;
        RequestedByAccountId = requestedByAccountId;
        FileHash = fileHash;
        ThirdPartyConsent = thirdPartyConsent;
        Provider = PlagiarismProviders.Copyleaks;
        Status = PlagiarismCheckStatuses.Pending;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public long ProductId { get; private set; }
    public long? EvidenceId { get; private set; }
    public string Provider { get; private set; } = PlagiarismProviders.Copyleaks;
    public string? ExternalScanId { get; private set; }
    public string Status { get; private set; } = PlagiarismCheckStatuses.Pending;
    public decimal? SimilarityScore { get; private set; }
    public int? MatchedSourceCount { get; private set; }
    public int? TotalWords { get; private set; }
    public int? CreditsUsed { get; private set; }
    public string FileHash { get; private set; } = string.Empty;
    public string? ReportUrl { get; private set; }
    public string? ReportObjectKey { get; private set; }
    public string? ErrorMessage { get; private set; }
    public long RequestedByAccountId { get; private set; }
    public bool ThirdPartyConsent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public void StartSending(string scanId)
    {
        ExternalScanId = scanId;
        Status = PlagiarismCheckStatuses.Sending;
        StartedAt = DateTimeOffset.UtcNow;
        ErrorMessage = null;
    }

    public void MarkProcessing() => Status = PlagiarismCheckStatuses.Processing;

    public void Complete(decimal score, int sourceCount, int totalWords, int credits, string? reportUrl)
    {
        SimilarityScore = Math.Clamp(score, 0, 100);
        MatchedSourceCount = Math.Max(0, sourceCount);
        TotalWords = Math.Max(0, totalWords);
        CreditsUsed = Math.Max(0, credits);
        ReportUrl = reportUrl;
        Status = PlagiarismCheckStatuses.Completed;
        ErrorMessage = null;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    public void Fail(string message)
    {
        Status = PlagiarismCheckStatuses.Failed;
        ErrorMessage = message.Length <= 2000 ? message : message[..2000];
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
