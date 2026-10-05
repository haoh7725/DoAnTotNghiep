using ResearchManagement.Application.Common;
using ResearchManagement.Application.ProductTypes;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Application.ProductTypes.Models;
using ResearchManagement.Domain.Common;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Tests;

public class ProductTypeServiceTests
{
    private readonly FakeRepository _repo = new();
    private readonly ProductTypeService _sut;

    public ProductTypeServiceTests()
    {
        _sut = new ProductTypeService(_repo);
        _repo.Seed("BAI_BAO", "Bài báo");
    }

    [Fact]
    public async Task Create_NewCode_ReturnsTrimmedValues()
    {
        var result = await _sut.CreateAsync(new CreateProductTypeRequest { Code = "SANG_CHE", Name = "  Sáng chế " }, default);

        Assert.Equal("SANG_CHE", result.Code);
        Assert.Equal("Sáng chế", result.Name);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task Create_DuplicateCode_Conflicts() =>
        await Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CreateAsync(new CreateProductTypeRequest { Code = "BAI_BAO", Name = "Trùng" }, default));

    [Fact]
    public async Task Update_ChangesNameButNeverTheCode()
    {
        var id = (await _sut.ListAsync(default)).Single().Id;

        var result = await _sut.UpdateAsync(id, new UpdateProductTypeRequest { Name = "Bài báo khoa học" }, default);

        Assert.Equal("BAI_BAO", result.Code);
        Assert.Equal("Bài báo khoa học", result.Name);
    }

    [Fact]
    public async Task GetUpdateDelete_UnknownId_AreNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(99, default));
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.UpdateAsync(99, new UpdateProductTypeRequest { Name = "X" }, default));
        await Assert.ThrowsAsync<NotFoundException>(() => _sut.DeleteAsync(99, default));
    }

    private sealed class FakeRepository : IProductTypeRepository
    {
        private readonly List<ProductType> _items = [];
        private long _nextId = 1;

        public void Seed(string code, string name) => Add(new ProductType(code, name));

        private void Add(ProductType item)
        {
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(item, _nextId++);
            _items.Add(item);
        }

        public Task<IReadOnlyList<ProductType>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProductType>>(_items.OrderBy(x => x.Code).ToList());

        public Task<ProductType?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Any(x => x.Code == code));

        public Task AddAsync(ProductType productType, CancellationToken cancellationToken)
        {
            Add(productType);
            return Task.CompletedTask;
        }

        public void Remove(ProductType productType) => _items.Remove(productType);
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
