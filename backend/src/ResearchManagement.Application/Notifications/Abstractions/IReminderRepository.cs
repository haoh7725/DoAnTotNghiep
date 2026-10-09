
using ResearchManagement.Application.Notifications.Models;

namespace ResearchManagement.Application.Notifications.Abstractions;

public interface IReminderRepository
{
    Task<List<ReminderCandidate>> GetCandidatesAsync(
        DateOnly today,
        CancellationToken cancellationToken);
}
