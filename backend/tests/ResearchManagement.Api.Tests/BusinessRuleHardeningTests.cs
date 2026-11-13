using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Products;

namespace ResearchManagement.Api.Tests;

public sealed class BusinessRuleHardeningTests
{
    [Fact]
    public void New_article_progress_starts_at_writing_and_requires_evidence_for_next_step()
    {
        Assert.Equal(0, ArticleProgressPolicy.Rank(null));
        Assert.Throws<BusinessRuleException>(() => ArticleProgressPolicy.ValidateTransition("DANG_VIET", "DANG_PHAN_BIEN", 0));
        ArticleProgressPolicy.ValidateTransition("DANG_VIET", "DANG_PHAN_BIEN", 1);
    }

    [Fact]
    public void Article_progress_cannot_skip_or_move_back()
    {
        Assert.Throws<BusinessRuleException>(() => ArticleProgressPolicy.ValidateTransition("DANG_VIET", "DA_NHAN_XET", 1));
        Assert.Throws<BusinessRuleException>(() => ArticleProgressPolicy.ValidateTransition("DA_NHAN_XET", "DANG_PHAN_BIEN", 1));
    }

    [Fact]
    public void Article_progress_can_change_after_submission_without_unlocking_content()
    {
        var product = new Product(1, 2, "Bài báo kiểm thử", null, null, null, "DANG_PHAN_BIEN");
        product.Submit();

        product.UpdateArticleProgress("DA_XUAT_BAN", new DateOnly(2026, 11, 10), "Tạp chí kiểm thử");

        Assert.Equal(ProductStatuses.Submitted, product.Status);
        Assert.Equal("DA_XUAT_BAN", product.ArticleStatus);
        Assert.Equal(new DateOnly(2026, 11, 10), product.PublishedDate);
    }

    [Fact]
    public void Approved_product_cannot_change_article_progress()
    {
        var product = new Product(1, 2, "Bài báo kiểm thử", null, null,
            new DateOnly(2026, 11, 10), "DA_XUAT_BAN");
        product.Submit();
        product.Approve(ProductStatuses.DepartmentHeadApproved);
        product.Approve(ProductStatuses.FacultyDeanApproved);
        product.Approve(ProductStatuses.Approved);

        Assert.Throws<InvalidOperationException>(() =>
            product.UpdateArticleProgress("DANG_PHAN_BIEN", null, null));
    }

    [Fact]
    public void Research_norm_supports_hour_and_point_thresholds()
    {
        var norm = new ResearchNorm(1, 2, 200m, "Quy chế kiểm thử", 80m);

        norm.Update(180m, "Điều chỉnh", 75m);

        Assert.Equal(180m, norm.RequiredHours);
        Assert.Equal(75m, norm.RequiredPoints);
    }
}
