using ResearchManagement.Api.Infrastructure;
using ResearchManagement.Application.Products;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Api.Tests;

public sealed class AnnualResearchTests
{
    [Theory]
    [InlineData(585.9999, "Chưa hoàn thành")]
    [InlineData(586, "Hoàn thành")]
    [InlineData(1172, "Hoàn thành")]
    public void Hour_threshold_is_independent_of_target_completion(decimal hours, string classification)
    {
        var line = new AnnualResearchService.Line(1, 1, "Bài báo", "Công trình", 10, 1, "QD2023_BB_SCOPUS", hours, 1, hours, "Quy đổi");
        var target = new AnnualResearchService.Target(10, "Bài báo", 10, 1, 1, 10, false, hours);
        var result = new AnnualResearchService.Result(1, "GV", "GV01", 1, [line], [target], []);
        Assert.Equal(classification, result.Classification);
        Assert.Equal(0, result.CompletedTargets);
        Assert.Equal(1, result.AssignedTargets);
        Assert.Equal(10, result.Targets[0].Percent);
    }

    [Fact]
    public void Final_recognition_is_dean_or_substitute_when_dean_is_an_author()
    {
        Assert.Equal((Roles.FacultyDean, ProductStatuses.Approved), ProductReviewPolicy.Next(ProductStatuses.DepartmentHeadApproved, false, false));
        Assert.Equal((Roles.Admin, ProductStatuses.Approved), ProductReviewPolicy.Next(ProductStatuses.DepartmentHeadApproved, false, true));
    }

    [Fact]
    public void Award_proposal_uses_top_ten_then_remaining_top_thirty_without_persistence()
    {
        var rows = Enumerable.Range(1, 10).Select(index => new AnnualResearchService.Result(
            index, $"Giảng viên {index}", $"GV{index:00}", 1,
            [new(index, 1, "Bài báo", "Công trình", null, 1, "QD2023", 1000 - index, 1, 1000 - index, "Căn cứ")],
            [], [])).ToList();

        var proposal = AnnualAwardProposal.Build(rows);

        Assert.Equal(1, proposal.AdvancedLaborQuota);
        Assert.Equal(3, proposal.TopThirtyQuota);
        Assert.Equal(2, proposal.EmulationFighterQuota);
        Assert.Equal(AnnualAwardProposal.AdvancedLaborTitle, proposal.Candidates[0].ProposedTitle);
        Assert.All(proposal.Candidates.Skip(1).Take(2), candidate =>
            Assert.Equal(AnnualAwardProposal.EmulationFighterTitle, candidate.ProposedTitle));
        Assert.Null(proposal.Candidates[3].ProposedTitle);
    }

    [Fact]
    public void Small_faculty_rounds_up_the_display_quota()
    {
        var rows = Enumerable.Range(1, 3).Select(index => new AnnualResearchService.Result(
            index, $"GV {index}", $"GV{index}", 1,
            [new(index, 1, "Bài báo", "Công trình", null, 1, "QD2023", 700 - index, 1, 700 - index, "Căn cứ")],
            [], [])).ToList();

        var proposal = AnnualAwardProposal.Build(rows);

        Assert.Equal(1, proposal.AdvancedLaborQuota);
        Assert.Equal(1, proposal.TopThirtyQuota);
        Assert.Equal(0, proposal.EmulationFighterQuota);
    }
}
