namespace ResearchManagement.Application.Plagiarism.Models;

public sealed record CreatePlagiarismCheckRequest(long EvidenceId, bool Consent);

public sealed record PlagiarismCheckResponse(
    long Id,
    long ProductId,
    long? EvidenceId,
    string Provider,
    string Status,
    decimal? SimilarityScore,
    int? MatchedSourceCount,
    int? TotalWords,
    int? CreditsUsed,
    string? ReportUrl,
    string? ErrorMessage,
    long RequestedByAccountId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt);

public sealed record PlagiarismSubmission(
    string ScanId,
    string FileName,
    byte[] Content,
    string DeveloperPayload);

public sealed record CopyleaksWebhookResult(
    string ScanId,
    decimal SimilarityScore,
    int SourceCount,
    int TotalWords,
    int CreditsUsed,
    string? ReportUrl);
