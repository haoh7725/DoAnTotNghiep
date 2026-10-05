namespace ResearchManagement.Application.Common;

internal static class TextNormalizer
{
    /// <summary>Cắt khoảng trắng hai đầu; chuỗi rỗng hoặc chỉ có khoảng trắng thành null.</summary>
    public static string? Clean(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
