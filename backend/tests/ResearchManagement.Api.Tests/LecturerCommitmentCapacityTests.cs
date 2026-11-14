using ResearchManagement.Application.Common;
using ResearchManagement.Application.ResearchTargetAllocations;

namespace ResearchManagement.Api.Tests;

public sealed class LecturerCommitmentCapacityTests
{
    [Theory]
    [InlineData(3, 1, 2)]
    [InlineData(2, 2, 0)]
    [InlineData(0, 0, 0)]
    public void AvailableArticles_NeverDropsBelowZero(
        int committed,
        int assigned,
        int expected)
    {
        var capacity = new LecturerCommitmentCapacity(committed, assigned);
        Assert.Equal(expected, capacity.AvailableArticles);
    }

    [Fact]
    public void Assignment_does_not_require_a_commitment()
    {
        var capacity = new LecturerCommitmentCapacity(3, 1);
        capacity.EnsureCanAssign(2);
        capacity.EnsureCanAssign(3);
        new LecturerCommitmentCapacity(0, 0).EnsureCanAssign(10);
        Assert.Throws<BusinessRuleException>(() => capacity.EnsureCanAssign(0));
    }
}
