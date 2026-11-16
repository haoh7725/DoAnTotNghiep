using ResearchManagement.Application.Common;

namespace ResearchManagement.Application.AcademicYears;

public static class AcademicYearSettingsPolicy
{
    public static void EnsureCanUpdate(
        bool hasFinalizedResults,
        DateOnly currentStart,
        DateOnly currentEnd,
        decimal currentHours,
        DateOnly requestedStart,
        DateOnly requestedEnd,
        decimal requestedHours)
    {
        if (!hasFinalizedResults) return;
        if (currentStart != requestedStart || currentEnd != requestedEnd)
            throw new ConflictException("Năm học đã có kết quả chốt; không được đổi khoảng thời gian ghi nhận.");
        if (currentHours != requestedHours)
            throw new ConflictException("Năm học đã có kết quả chốt; không được đổi định mức giờ NCKH.");
    }
}
