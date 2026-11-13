using ResearchManagement.Application.Common;

namespace ResearchManagement.Application.Evaluations;

public static class EvaluationMetric
{
    public static decimal Percent(string unit, decimal hours, decimal points, decimal requiredHours, decimal requiredPoints)
    {
        var required = unit switch
        {
            "GIO" => requiredHours,
            "DIEM" => requiredPoints,
            _ => throw new BusinessRuleException("Vui lòng chọn đánh giá theo Giờ NCKH hoặc Điểm NCKH.")
        };
        if (required <= 0)
            throw new BusinessRuleException($"Chưa có định mức {(unit == "GIO" ? "giờ" : "điểm")} hợp lệ cho phương thức đánh giá đã chọn.");
        return decimal.Round((unit == "GIO" ? hours : points) * 100m / required, 2);
    }
}
