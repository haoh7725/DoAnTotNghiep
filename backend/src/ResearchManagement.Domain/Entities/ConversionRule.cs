using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

public sealed class ConversionRule : BaseEntity
{
    private ConversionRule() { }
    public ConversionRule(long productTypeId, string code, int version, string name, string conditionDescription,
        decimal conversionValue, string unit, string authorRuleDescription, DateOnly effectiveFrom,
        DateOnly? effectiveTo, string legalBasis) => Update(productTypeId, code, version, name,
            conditionDescription, conversionValue, unit, authorRuleDescription, effectiveFrom, effectiveTo, legalBasis);

    public long ProductTypeId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string ConditionDescription { get; private set; } = string.Empty;
    public decimal ConversionValue { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public string AuthorRuleDescription { get; private set; } = string.Empty;
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string LegalBasis { get; private set; } = string.Empty;

    public void Update(long productTypeId, string code, int version, string name, string conditionDescription,
        decimal conversionValue, string unit, string authorRuleDescription, DateOnly effectiveFrom,
        DateOnly? effectiveTo, string legalBasis)
    {
        ProductTypeId = productTypeId; Code = code.Trim(); Version = version; Name = name.Trim();
        ConditionDescription = conditionDescription.Trim(); ConversionValue = conversionValue;
        Unit = unit.Trim().ToUpperInvariant(); AuthorRuleDescription = authorRuleDescription.Trim();
        EffectiveFrom = effectiveFrom; EffectiveTo = effectiveTo; LegalBasis = legalBasis.Trim();
    }
}

public sealed class ConversionCriterion : BaseEntity
{
    private ConversionCriterion() { }
    public ConversionCriterion(long conversionRuleId, string field, string @operator, string? stringValue, decimal? numericValue)
    { ConversionRuleId = conversionRuleId; Field = field; Operator = @operator; StringValue = stringValue?.Trim(); NumericValue = numericValue; }
    public long ConversionRuleId { get; private set; }
    public string Field { get; private set; } = string.Empty;
    public string Operator { get; private set; } = string.Empty;
    public string? StringValue { get; private set; }
    public decimal? NumericValue { get; private set; }
}

public sealed class AuthorConversionCoefficient
{
    private AuthorConversionCoefficient() { }
    public AuthorConversionCoefficient(long conversionRuleId, string authorRole, decimal coefficient)
    { ConversionRuleId = conversionRuleId; AuthorRole = authorRole.Trim(); Coefficient = coefficient; }
    public long ConversionRuleId { get; private set; }
    public string AuthorRole { get; private set; } = string.Empty;
    public decimal Coefficient { get; private set; }
}
