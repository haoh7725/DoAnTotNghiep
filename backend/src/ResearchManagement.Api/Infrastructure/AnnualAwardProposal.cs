namespace ResearchManagement.Api.Infrastructure;

public static class AnnualAwardProposal
{
    public const string AdvancedLaborTitle = "Lao động tiên tiến";
    public const string EmulationFighterTitle = "Chiến sĩ thi đua";

    public sealed record Candidate(long LecturerId, string LecturerName, string LecturerCode,
        decimal TotalHours, int Rank, string? ProposedTitle, bool TiedAtBoundary);

    public sealed record Result(int Population, int Eligible, int AdvancedLaborQuota,
        int TopThirtyQuota, int EmulationFighterQuota, IReadOnlyList<Candidate> Candidates);

    public static Result Build(IReadOnlyList<AnnualResearchService.Result> rows)
    {
        var population = rows.Count;
        var eligible = rows.Where(x => x.TotalHours >= AnnualResearchService.RequiredHours && x.Issues.Count == 0)
            .OrderByDescending(x => x.TotalHours).ThenBy(x => x.LecturerCode).ToList();
        var advancedQuota = population == 0 ? 0 : (int)Math.Ceiling(population * 0.10m);
        var topThirtyQuota = population == 0 ? 0 : (int)Math.Ceiling(population * 0.30m);
        var advancedCount = Math.Min(advancedQuota, eligible.Count);
        var topThirtyCount = Math.Min(Math.Max(topThirtyQuota, advancedCount), eligible.Count);
        var advancedCutoff = advancedCount > 0 ? eligible[advancedCount - 1].TotalHours : (decimal?)null;
        var topThirtyCutoff = topThirtyCount > advancedCount ? eligible[topThirtyCount - 1].TotalHours : (decimal?)null;

        var candidates = eligible.Select((x, index) =>
        {
            var title = index < advancedCount ? AdvancedLaborTitle :
                index < topThirtyCount ? EmulationFighterTitle : null;
            var tiedAtAdvanced = advancedCutoff.HasValue && x.TotalHours == advancedCutoff &&
                eligible.Count(item => item.TotalHours == advancedCutoff) > 1;
            var tiedAtTopThirty = topThirtyCutoff.HasValue && x.TotalHours == topThirtyCutoff &&
                eligible.Count(item => item.TotalHours == topThirtyCutoff) > 1;
            return new Candidate(x.LecturerId, x.LecturerName, x.LecturerCode, x.TotalHours,
                index + 1, title, tiedAtAdvanced || tiedAtTopThirty);
        }).ToList();

        return new(population, eligible.Count, advancedQuota, topThirtyQuota,
            Math.Max(0, topThirtyQuota - advancedQuota), candidates);
    }
}
