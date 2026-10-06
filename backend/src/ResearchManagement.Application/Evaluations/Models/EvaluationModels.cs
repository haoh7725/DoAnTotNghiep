namespace ResearchManagement.Application.Evaluations.Models;

/// <summary>Hợp đồng tóm tắt kết quả dùng chung cho Web và Mobile.</summary>
public sealed record EvaluationSummaryResponse(
    long Id,
    long LecturerId,
    long AcademicYearId,
    string AcademicYearCode,
    int Attempt,
    string Status,
    string? Classification,
    decimal TotalHours,
    decimal TotalPoints,
    DateTimeOffset EvaluatedAt);

/// <summary>Chi tiết một sản phẩm đã tham gia vào kết quả quy đổi.</summary>
public sealed record ConversionDetailResponse(
    long Id,
    long ProductId,
    string ProductTitle,
    long ProductTypeId,
    string ProductTypeName,
    long ConversionRuleId,
    string RuleCode,
    int RuleVersion,
    decimal BaseValue,
    decimal AuthorCoefficient,
    decimal ConvertedValue,
    string Unit,
    string CalculationBasis);

/// <summary>
/// Kết quả đầy đủ. Thông tin giảng viên là ảnh chụp tại thời điểm đánh giá để lịch sử không đổi
/// khi hồ sơ hiện tại được cập nhật.
/// </summary>
public sealed record EvaluationDetailResponse(
    long Id,
    long LecturerId,
    long AcademicYearId,
    string AcademicYearCode,
    int Attempt,
    string Status,
    string? Classification,
    string? ClassificationBasis,
    string LecturerCodeSnapshot,
    string LecturerNameSnapshot,
    long DepartmentIdSnapshot,
    string? AcademicRankSnapshot,
    string? DegreeSnapshot,
    decimal TotalHours,
    decimal TotalPoints,
    DateTimeOffset EvaluatedAt,
    IReadOnlyList<ConversionDetailResponse> Details);
