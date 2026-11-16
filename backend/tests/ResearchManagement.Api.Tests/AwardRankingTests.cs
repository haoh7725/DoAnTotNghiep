using ResearchManagement.Api.Infrastructure;

namespace ResearchManagement.Api.Tests;

public sealed class AwardRankingTests
{
    private static AnnualResearchService.Result Row(int id, decimal hours, params string[] issues) =>
        new(id, $"GV {id}", $"GV{id:000}", 1, 586,
            [new(id, 1, "Bài báo", "Công trình", null, 1, "QD2023", hours, 1, hours, "Căn cứ")], [], issues.ToList());

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1, 1, 1, 0)]
    [InlineData(3, 1, 1, 0)]
    [InlineData(4, 1, 2, 1)]
    [InlineData(10, 1, 3, 2)]
    [InlineData(11, 2, 4, 2)]
    [InlineData(100, 10, 30, 20)]
    public void Exclusive_awards_use_ceiling_of_faculty_population(int count, int fighters, int topThirty, int labor)
    {
        var proposal = AnnualAwardProposal.Build(Enumerable.Range(1, count).Select(i => Row(i, 2000 - i)).ToList());
        Assert.Equal(fighters, proposal.EmulationFighterQuota);
        Assert.Equal(topThirty, proposal.TopThirtyQuota);
        Assert.Equal(labor, proposal.AdvancedLaborQuota);
        Assert.Equal(fighters, proposal.Candidates.Count(c => c.ProposedTitle == AnnualAwardProposal.EmulationFighterTitle));
        Assert.Equal(labor, proposal.Candidates.Count(c => c.ProposedTitle == AnnualAwardProposal.AdvancedLaborTitle));
        Assert.Equal(count, proposal.Candidates.Select(c => c.LecturerId).Distinct().Count());
        Assert.All(proposal.Candidates.Take(fighters), c => Assert.Equal(AnnualAwardProposal.EmulationFighterTitle, c.ProposedTitle));
        Assert.All(proposal.Candidates.Skip(fighters).Take(labor), c => Assert.Equal(AnnualAwardProposal.AdvancedLaborTitle, c.ProposedTitle));
        Assert.All(proposal.Candidates.Skip(topThirty), c => Assert.Null(c.ProposedTitle));
    }

    [Fact]
    public void Threshold_and_conversion_issues_filter_before_ranking_without_changing_hours()
    {
        var rows = new[] { Row(1, 585.9999m), Row(2, 586m), Row(3, 2000, "Thiếu quy đổi"), Row(4, 0) };
        var before = rows.Select(r => (r.TotalHours, r.Classification, r.CompletedTargets)).ToArray();
        var result = AnnualAwardProposal.Build(rows);
        Assert.Equal(4, result.Population);
        Assert.Equal(1, result.Eligible);
        Assert.Equal(2, Assert.Single(result.Candidates).LecturerId);
        Assert.Equal(AnnualAwardProposal.EmulationFighterTitle, result.Candidates[0].ProposedTitle);
        Assert.Equal(before, rows.Select(r => (r.TotalHours, r.Classification, r.CompletedTargets)).ToArray());
    }

    [Fact]
    public void Ties_keep_existing_code_order_and_boundary_warning_without_expanding_quota()
    {
        var rows = new[] { Row(5, 600), Row(4, 900), Row(3, 900), Row(2, 1000), Row(1, 1000),
            Row(6, 599), Row(7, 598), Row(8, 597), Row(9, 596), Row(10, 595) };
        var result = AnnualAwardProposal.Build(rows);
        Assert.Equal(new long[] { 1, 2, 3, 4 }, result.Candidates.Take(4).Select(c => c.LecturerId));
        Assert.All(result.Candidates.Take(4), c => Assert.True(c.TiedAtBoundary));
        Assert.False(result.Candidates[4].TiedAtBoundary);
        Assert.Equal(AnnualAwardProposal.EmulationFighterTitle, result.Candidates[0].ProposedTitle);
        Assert.All(result.Candidates.Skip(1).Take(2), c => Assert.Equal(AnnualAwardProposal.AdvancedLaborTitle, c.ProposedTitle));
        Assert.Null(result.Candidates[3].ProposedTitle);
    }
}
