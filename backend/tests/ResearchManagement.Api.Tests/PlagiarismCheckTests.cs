using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Tests;

public sealed class PlagiarismCheckTests
{
    [Fact]
    public void New_check_starts_pending_and_records_consent()
    {
        var item = new PlagiarismCheck(10, 20, 30, new string('a', 64), true);

        Assert.Equal(PlagiarismCheckStatuses.Pending, item.Status);
        Assert.Equal(PlagiarismProviders.Copyleaks, item.Provider);
        Assert.True(item.ThirdPartyConsent);
        Assert.Null(item.SimilarityScore);
    }

    [Fact]
    public void Completed_check_normalizes_provider_values()
    {
        var item = new PlagiarismCheck(10, 20, 30, new string('a', 64), true);
        item.StartSending("rh-1-test");
        item.MarkProcessing();
        item.Complete(110m, -1, -2, -3, "https://example.test/report");

        Assert.Equal(PlagiarismCheckStatuses.Completed, item.Status);
        Assert.Equal(100m, item.SimilarityScore);
        Assert.Equal(0, item.MatchedSourceCount);
        Assert.Equal(0, item.TotalWords);
        Assert.Equal(0, item.CreditsUsed);
        Assert.NotNull(item.CompletedAt);
    }
}
