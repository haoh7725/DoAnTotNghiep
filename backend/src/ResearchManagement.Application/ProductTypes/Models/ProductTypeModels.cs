using System.ComponentModel.DataAnnotations;

namespace ResearchManagement.Application.ProductTypes.Models;

public sealed class CreateProductTypeRequest
{
    /// <summary>Chữ hoa không dấu, số và gạch dưới, bắt đầu bằng chữ. Không đổi được sau khi tạo.</summary>
    [Required, StringLength(30),
     RegularExpression("^[A-Z][A-Z0-9_]*$", ErrorMessage = "Mã loại gồm chữ hoa không dấu, số, gạch dưới và bắt đầu bằng chữ.")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateProductTypeRequest
{
    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;
}

public sealed record ProductTypeResponse(long Id, string Code, string Name);
