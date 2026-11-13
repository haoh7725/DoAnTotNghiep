using ResearchManagement.Application.Common;

namespace ResearchManagement.Application.ResearchTargets;

public static class TargetRules
{
    public static void EnsureOpen(DateOnly today, DateOnly yearStart, DateOnly yearEnd, DateOnly? deadline)
    {
        if (today < yearStart || today > yearEnd)
            throw new BusinessRuleException("Không thể giao hoặc liên kết thêm bài ngoài năm học của chỉ tiêu.");
        if (deadline.HasValue && today > deadline.Value)
            throw new BusinessRuleException("Chỉ tiêu đã quá hạn. Phòng QLKH cần gia hạn có lý do trước khi tiếp tục phân công hoặc liên kết bài.");
    }

    public static void EnsurePublicationMatches(long? requiredPublication, long? actualPublication)
    {
        if (requiredPublication.HasValue && requiredPublication != actualPublication)
            throw new BusinessRuleException("Nơi công bố không khớp nơi công bố cụ thể của chỉ tiêu.");
    }
}
