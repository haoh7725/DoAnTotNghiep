using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Tests;

public sealed class ConversionSharingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(7)]
    public void Sharing_conserves_total_hours(int authors)
    {
        var main = ConversionRule.SharedCoefficient(true, authors);
        var coAuthor = ConversionRule.SharedCoefficient(false, authors);
        Assert.InRange(main + (authors - 1) * coAuthor, 0.99999999999999999999999999m, 1.00000000000000000000000001m);
    }

    [Fact]
    public void Three_authors_share_881_hours_without_giving_everyone_the_whole_amount()
    {
        Assert.Equal(489.4444m, decimal.Round(881m * ConversionRule.SharedCoefficient(true, 3), 4));
        Assert.Equal(195.7778m, decimal.Round(881m * ConversionRule.SharedCoefficient(false, 3), 4));
    }

    [Fact]
    public void Cannot_calculate_without_authors() => Assert.Throws<ArgumentOutOfRangeException>(() => ConversionRule.SharedCoefficient(true, 0));

    [Fact]
    public void External_authors_count_is_validated()
    {
        var product = new Product(1, 1, "Kiểm thử chia giờ", null, null, null);
        product.SetExternalAuthorCount(2);
        Assert.Equal(2, product.ExternalAuthorCount);
        Assert.Throws<ArgumentOutOfRangeException>(() => product.SetExternalAuthorCount(-1));
    }
}
