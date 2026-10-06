using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Api.Contracts;

public sealed record ConversionCriterionRequest([Required] string Field, [Required] string Operator, string? StringValue, decimal? NumericValue);
public sealed record AuthorCoefficientRequest([Required, StringLength(100)] string AuthorRole, [Range(0, double.MaxValue)] decimal Coefficient);
public sealed record ConversionRuleRequest(
    [Range(1, long.MaxValue)] long ProductTypeId,
    [Required, StringLength(50)] string Code,
    [Range(1, int.MaxValue)] int Version,
    [Required, StringLength(300)] string Name,
    [Required] string ConditionDescription,
    [Range(0, double.MaxValue)] decimal ConversionValue,
    [Required, RegularExpression("^(GIO|DIEM)$")] string Unit,
    [Required] string AuthorRuleDescription,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    [Required, StringLength(512)] string LegalBasis,
    List<ConversionCriterionRequest> Criteria,
    List<AuthorCoefficientRequest> AuthorCoefficients);
