using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Tests;

public sealed class ResearchTargetTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_RejectsNonPositiveArticleCount(int articleCount)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ResearchTarget(1, 1, articleCount, new DateOnly(2027, 6, 30)));
    }

    [Fact]
    public void Update_RejectsZeroAndPreservesExistingCount()
    {
        var target = new ResearchTarget(1, 1, 12, new DateOnly(2027, 6, 30));

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            target.Update(0, new DateOnly(2027, 5, 31)));

        Assert.Equal(12, target.AssignedArticleCount);
    }
}
