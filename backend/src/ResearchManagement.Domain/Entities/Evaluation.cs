using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

public sealed class Evaluation : BaseEntity
{
    private Evaluation() { }

    public Evaluation(long lecturerId, long academicYearId, int attempt, long evaluatorAccountId,
        string lecturerCode, string lecturerName, long departmentId, string? academicRank, string? degree,
        string evaluationUnit = "GIO")
    {
        LecturerId = lecturerId;
        AcademicYearId = academicYearId;
        Attempt = attempt;
        EvaluatorAccountId = evaluatorAccountId;
        Status = "NHAP";
        LecturerCodeSnapshot = lecturerCode;
        LecturerNameSnapshot = lecturerName;
        DepartmentIdSnapshot = departmentId;
        AcademicRankSnapshot = academicRank;
        DegreeSnapshot = degree;
        EvaluatedAt = DateTimeOffset.UtcNow;
        SelectUnit(evaluationUnit);
    }

    public long LecturerId { get; private set; }
    public long AcademicYearId { get; private set; }
    public int Attempt { get; private set; }
    public long EvaluatorAccountId { get; private set; }
    public string Status { get; private set; } = "NHAP";
    public string? Classification { get; private set; }
    public string? ClassificationBasis { get; private set; }
    public string LecturerCodeSnapshot { get; private set; } = string.Empty;
    public string LecturerNameSnapshot { get; private set; } = string.Empty;
    public long DepartmentIdSnapshot { get; private set; }
    public string? AcademicRankSnapshot { get; private set; }
    public string? DegreeSnapshot { get; private set; }
    public DateTimeOffset EvaluatedAt { get; private set; }
    public string EvaluationUnit { get; private set; } = "GIO";

    public void SelectUnit(string unit)
    {
        if (Status == "CHOT") throw new InvalidOperationException("Không được đổi phương thức của kết quả đã chốt.");
        if (unit is not ("GIO" or "DIEM")) throw new ArgumentException("Phương thức phải là GIO hoặc DIEM.", nameof(unit));
        EvaluationUnit = unit;
    }

    public void Finalize(string classification, string basis)
    {
        if (Status == "CHOT") throw new InvalidOperationException("Kết quả đã được chốt.");
        Classification = classification.Trim();
        ClassificationBasis = basis.Trim();
        Status = "CHOT";
        EvaluatedAt = DateTimeOffset.UtcNow;
    }

    public void RecalculateClassification(string classification, string basis)
    {
        if (Status != "CHOT") throw new InvalidOperationException("Chỉ cập nhật được kết quả đã chốt.");
        Classification = classification.Trim();
        ClassificationBasis = basis.Trim();
        EvaluatedAt = DateTimeOffset.UtcNow;
    }
}

public sealed class EvaluationConversionDetail : BaseEntity
{
    private EvaluationConversionDetail() { }

    public EvaluationConversionDetail(long evaluationId, long lecturerId, long academicYearId,
        long productId, long productTypeId, long conversionRuleId, decimal baseValue,
        decimal authorCoefficient, string unit, string calculationBasis)
    {
        EvaluationId = evaluationId;
        LecturerId = lecturerId;
        AcademicYearId = academicYearId;
        ProductId = productId;
        ProductTypeId = productTypeId;
        ConversionRuleId = conversionRuleId;
        BaseValue = baseValue;
        AuthorCoefficient = authorCoefficient;
        ConvertedValue = decimal.Round(baseValue * authorCoefficient, 4, MidpointRounding.AwayFromZero);
        Unit = unit;
        CalculationBasis = calculationBasis;
    }

    public long EvaluationId { get; private set; }
    public long LecturerId { get; private set; }
    public long AcademicYearId { get; private set; }
    public long ProductId { get; private set; }
    public long ProductTypeId { get; private set; }
    public long ConversionRuleId { get; private set; }
    public decimal BaseValue { get; private set; }
    public decimal AuthorCoefficient { get; private set; }
    public decimal ConvertedValue { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string CalculationBasis { get; private set; } = string.Empty;
}
