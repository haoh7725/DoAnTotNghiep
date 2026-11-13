using ResearchManagement.Application.Common;

namespace ResearchManagement.Application.Products;

public static class ProductRecognitionRules
{
    public static void Validate(DateOnly? date, DateOnly today, string productType, string? articleStatus, int evidenceCount)
    {
        if (!date.HasValue) throw new BusinessRuleException("Phải bổ sung ngày công bố/công nhận/nghiệm thu trước khi công nhận sản phẩm.");
        if (date.Value > today) throw new BusinessRuleException("Ngày công bố/công nhận/nghiệm thu không được nằm trong tương lai.");
        if (productType == "BAI_BAO" && articleStatus != "DA_XUAT_BAN")
            throw new BusinessRuleException("Bài báo chỉ được công nhận khi đã xuất bản.");
        if (evidenceCount == 0) throw new BusinessRuleException("Phải có ít nhất một minh chứng trước khi công nhận sản phẩm.");
    }
}
