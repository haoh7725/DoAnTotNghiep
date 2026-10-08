using ResearchManagement.Application.Common;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Application.ProductTypes.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.ProductTypes;

/// <summary>
/// Danh mục loại sản phẩm. Mọi người dùng đăng nhập được đọc; quyền ghi do lớp Api giới hạn cho Quản trị.
/// </summary>
public sealed class ProductTypeService(IProductTypeRepository productTypes)
{
    public async Task<IReadOnlyList<ProductTypeResponse>> ListAsync(CancellationToken cancellationToken) =>
        (await productTypes.ListAsync(cancellationToken)).Select(ToResponse).ToList();

    public async Task<ProductTypeResponse> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        ToResponse(await FindAsync(id, cancellationToken));

    public async Task<ProductTypeResponse> CreateAsync(
        CreateProductTypeRequest request, CancellationToken cancellationToken)
    {
        var code = request.Code.Trim();
        if (await productTypes.ExistsByCodeAsync(code, cancellationToken))
            throw new ConflictException("Mã loại sản phẩm đã tồn tại.");

        var productType = new ProductType(code, request.Name.Trim());
        await productTypes.AddAsync(productType, cancellationToken);
        await productTypes.SaveChangesAsync(cancellationToken);
        return ToResponse(productType);
    }

    /// <summary>Chỉ đổi được tên hiển thị; mã loại giữ nguyên vì giao diện và quy đổi dựa vào mã.</summary>
    public async Task<ProductTypeResponse> UpdateAsync(
        long id, UpdateProductTypeRequest request, CancellationToken cancellationToken)
    {
        var productType = await FindAsync(id, cancellationToken);
        productType.Rename(request.Name.Trim());
        await productTypes.SaveChangesAsync(cancellationToken);
        return ToResponse(productType);
    }

    /// <summary>Loại đã được sản phẩm, kế hoạch hoặc quy định quy đổi dùng sẽ bị khóa ngoại từ chối (409).</summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        var productType = await FindAsync(id, cancellationToken);
        productTypes.Remove(productType);
        await productTypes.SaveChangesAsync(cancellationToken);
    }

    private async Task<ProductType> FindAsync(long id, CancellationToken cancellationToken) =>
        await productTypes.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy loại sản phẩm.");

    private static ProductTypeResponse ToResponse(ProductType p) => new(p.Id, p.Code, p.Name);
}
