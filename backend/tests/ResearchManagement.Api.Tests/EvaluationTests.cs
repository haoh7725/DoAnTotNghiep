using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Tests;

public sealed class EvaluationTests
{
    [Fact]
    public void Finalize_ChangesDraftToFinalAndPreventsSecondFinalization()
    {
        var evaluation = new Evaluation(1, 2, 1, 3, "GV001", "Giảng viên kiểm thử", 4, null, null);

        evaluation.Finalize("Tốt", "Đủ giờ quy đổi theo quy định hiện hành.");

        Assert.Equal("CHOT", evaluation.Status);
        Assert.Equal("Tốt", evaluation.Classification);
        Assert.Throws<InvalidOperationException>(() => evaluation.Finalize("Khá", "Không hợp lệ"));
    }

    [Fact]
    public void ConversionDetail_AppliesAuthorCoefficientAndRoundsValue()
    {
        var detail = new EvaluationConversionDetail(1, 2, 3, 4, 5, 6, 12.34567m, 0.8m, "GIO", "Tác giả chính");

        Assert.Equal(9.8765m, detail.ConvertedValue);
        Assert.Equal("GIO", detail.Unit);
    }
}
