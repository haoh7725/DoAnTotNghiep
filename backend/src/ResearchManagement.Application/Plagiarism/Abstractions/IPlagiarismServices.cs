using ResearchManagement.Application.Plagiarism.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Plagiarism.Abstractions;

public interface IPlagiarismCheckRepository
{
    Task<PlagiarismCheck?> GetByIdAsync(long id, CancellationToken ct);
    Task<PlagiarismCheck?> GetByExternalScanIdAsync(string scanId, CancellationToken ct);
    Task<List<PlagiarismCheck>> GetByProductIdAsync(long productId, CancellationToken ct);
    Task<bool> HasActiveCheckAsync(long evidenceId, string fileHash, CancellationToken ct);
    Task AddAsync(PlagiarismCheck item, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IPlagiarismProvider
{
    Task SubmitAsync(PlagiarismSubmission submission, CancellationToken ct);
}
