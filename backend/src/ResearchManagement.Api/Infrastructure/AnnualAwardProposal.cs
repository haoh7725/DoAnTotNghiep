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
        var eligible = rows.Where(x => x.TotalHours >= x.RequiredHours && x.Issues.Count == 0)
            .OrderByDescending(x => x.TotalHours).ThenBy(x => x.LecturerCode).ToList();
        var fighterQuota = population == 0 ? 0 : (int)Math.Ceiling(population * 0.10m);
        var topThirtyQuota = population == 0 ? 0 : (int)Math.Ceiling(population * 0.30m);
        var fighterCount = Math.Min(fighterQuota, eligible.Count);
        var topThirtyCount = Math.Min(Math.Max(topThirtyQuota, fighterCount), eligible.Count);
        var fighterCutoff = fighterCount > 0 ? eligible[fighterCount - 1].TotalHours : (decimal?)null;
        var topThirtyCutoff = topThirtyCount > fighterCount ? eligible[topThirtyCount - 1].TotalHours : (decimal?)null;

        var candidates = eligible.Select((x, index) =>
        {
            var title = index < fighterCount ? EmulationFighterTitle :
                index < topThirtyCount ? AdvancedLaborTitle : null;
            var tiedAtFighter = fighterCutoff.HasValue && x.TotalHours == fighterCutoff &&
                eligible.Count(item => item.TotalHours == fighterCutoff) > 1;
            var tiedAtTopThirty = topThirtyCutoff.HasValue && x.TotalHours == topThirtyCutoff &&
                eligible.Count(item => item.TotalHours == topThirtyCutoff) > 1;
            return new Candidate(x.LecturerId, x.LecturerName, x.LecturerCode, x.TotalHours,
                index + 1, title, tiedAtFighter || tiedAtTopThirty);
        }).ToList();

        return new(population, eligible.Count, Math.Max(0, topThirtyQuota - fighterQuota), topThirtyQuota,
            fighterQuota, candidates);
    }
}
