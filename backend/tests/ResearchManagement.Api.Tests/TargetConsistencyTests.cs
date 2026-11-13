using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchTargets;

namespace ResearchManagement.Api.Tests;

public sealed class TargetConsistencyTests
{
    [Fact]
    public void General_target_accepts_specific_publication()
        => TargetRules.EnsurePublicationMatches(null, 42);

    [Fact]
    public void Specific_target_rejects_different_publication()
        => Assert.Throws<BusinessRuleException>(() => TargetRules.EnsurePublicationMatches(42, 43));

    [Fact]
    public void Target_rejects_assignment_after_deadline()
        => Assert.Throws<BusinessRuleException>(() => TargetRules.EnsureOpen(
            new(2026, 12, 1), new(2026, 8, 1), new(2027, 7, 31), new(2026, 11, 30)));

    [Fact]
    public void Target_allows_assignment_before_deadline()
        => TargetRules.EnsureOpen(new(2026, 11, 13), new(2026, 8, 1), new(2027, 7, 31), new(2027, 6, 30));
}
