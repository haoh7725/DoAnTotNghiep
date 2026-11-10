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
    public void EnsureCanAssign_RejectsCountAboveAvailableCapacity()
    {
        var capacity = new LecturerCommitmentCapacity(3, 1);
        capacity.EnsureCanAssign(2);
        var error = Assert.Throws<BusinessRuleException>(() => capacity.EnsureCanAssign(3));
        Assert.Contains("chỉ còn có thể nhận 2 bài", error.Message);
    }
}
