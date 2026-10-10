using ResearchManagement.Application.ResearchPlans.Models;

namespace ResearchManagement.Application.ResearchPlans.Abstractions;

public interface ILecturerAccessRepository
{
    Task<LecturerAccessInfo?> GetAccessInfoAsync(
        long lecturerId,
        CancellationToken cancellationToken);
}