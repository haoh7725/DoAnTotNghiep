using Microsoft.AspNetCore.Http;
using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Application.ProductTypes.Abstractions;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Domain.Common;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Tests;

public class EvidenceServiceTests
{
    private readonly FakeLecturers _lecturers = new();
    private readonly FakeProductTypes _types = new();
    private readonly FakeProducts _products;
    private readonly FakeProductEvidenceRepository _evidences = new();
    private readonly FakeFileStorageService _storage = new();

    public EvidenceServiceTests()
    {
        _products = new FakeProducts(_lecturers, _types);
    }

    private static FakeUser Lecturer(long accountId) =>
        new(accountId, new RoleAssignmentDto(Roles.Lecturer, Scopes.Personal, null, null));
    private static FakeUser Admin() =>
        new(901, new RoleAssignmentDto(Roles.Admin, Scopes.Global, null, null));
    private static FakeUser OtherUser() =>
        new(999, new RoleAssignmentDto(Roles.Lecturer, Scopes.Personal, null, null));

    private EvidenceService CreateService(ICurrentUser user) =>
        new(_products, _evidences, _storage, user);

    private static IFormFile CreateFormFile(string fileName, byte[] content, string contentType = "application/octet-stream")
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private async Task<Product> SeedProductAsync(
        long creatorAccountId = 101,
        string reviewStatus = ReviewStatuses.Draft)
    {
        var type = (await _types.GetByIdAsync(FakeProductTypes.ArticleId, default))!;
        var content = new ProductContent("Bài báo thử nghiệm minh chứng", null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null, null);
        var product = new Product(type.Id, 1, creatorAccountId, "BB-2026-TEST01", content, ArticleStatuses.Writing);
        typeof(Product).GetProperty(nameof(Product.ReviewStatus))!.SetValue(product, reviewStatus);
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(product, 1L);

        var authorSlot = new AuthorSlot(1, 10, AuthorRoles.Main, 1);
        await _products.CreateAsync(product, [authorSlot], default);
        return product;
    }

    [Fact]
    public async Task Upload_ValidPdfFile_SavesFileAndComputesSha256()
    {
        var product = await SeedProductAsync(creatorAccountId: 101);
        var service = CreateService(Lecturer(101));
        var bytes = "PDF file content demo"u8.ToArray();
        var file = CreateFormFile("minh_chung_bai_bao.pdf", bytes, "application/pdf");

        var result = await service.UploadAsync(product.Id, file, default);

        Assert.Equal("minh_chung_bai_bao.pdf", result.FileName);
        Assert.Equal("application/pdf", result.MimeType);
        Assert.Equal(bytes.Length, result.FileSizeBytes);
        Assert.NotNull(result.Sha256);
        Assert.Equal(64, result.Sha256.Length);
        Assert.True(_storage.Files.Count > 0);
    }

    [Fact]
    public async Task Upload_DisallowedExtension_ThrowsBusinessRuleException()
    {
        var product = await SeedProductAsync(creatorAccountId: 101);
        var service = CreateService(Lecturer(101));
        var file = CreateFormFile("malicious_script.exe", "dangerous"u8.ToArray());

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.UploadAsync(product.Id, file, default));

        Assert.Contains(".exe", ex.Message);
    }

    [Fact]
    public async Task Upload_EmptyFile_ThrowsBusinessRuleException()
    {
        var product = await SeedProductAsync(creatorAccountId: 101);
        var service = CreateService(Lecturer(101));
        var file = CreateFormFile("empty.pdf", []);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.UploadAsync(product.Id, file, default));

        Assert.Contains("trống", ex.Message);
    }

    [Fact]
    public async Task Upload_FileExceedingMaxSize_ThrowsBusinessRuleException()
    {
        var product = await SeedProductAsync(creatorAccountId: 101);
        var service = CreateService(Lecturer(101));
        var fakeBigStream = new MemoryStream();
        // Giả lập file lớn hơn 25 MB mà không tốn RAM thật
        var file = new FormFile(fakeBigStream, 0, EvidenceService.MaxFileSizeBytes + 1024, "file", "big.pdf");

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.UploadAsync(product.Id, file, default));

        Assert.Contains("vượt quá giới hạn", ex.Message);
    }

    [Fact]
    public async Task Upload_WhenProductNotEditable_ThrowsBusinessRuleException()
    {
        var product = await SeedProductAsync(creatorAccountId: 101, reviewStatus: ReviewStatuses.Pending);
        var service = CreateService(Lecturer(101));
        var file = CreateFormFile("paper.pdf", "content"u8.ToArray(), "application/pdf");

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => service.UploadAsync(product.Id, file, default));

        Assert.Contains("không thể thay đổi minh chứng", ex.Message);
    }

    [Fact]
    public async Task Upload_ByUnauthorizedUser_ThrowsForbiddenException()
    {
        var product = await SeedProductAsync(creatorAccountId: 101);
        var service = CreateService(OtherUser());
        var file = CreateFormFile("paper.pdf", "content"u8.ToArray(), "application/pdf");

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.UploadAsync(product.Id, file, default));
    }

    [Fact]
    public async Task GetByProductId_WhenUserCannotView_ThrowsForbiddenException()
    {
        var product = await SeedProductAsync(creatorAccountId: 101, reviewStatus: ReviewStatuses.Draft);
        var service = CreateService(OtherUser());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetByProductIdAsync(product.Id, default));
    }

    [Fact]
    public async Task GetFileForDownload_WhenUserCanView_ReturnsFileStream()
    {
        var product = await SeedProductAsync(creatorAccountId: 101);
        var uploadService = CreateService(Lecturer(101));
        var bytes = "Test evidence content for download"u8.ToArray();
        var file = CreateFormFile("tai_lieu.docx", bytes);
        var uploaded = await uploadService.UploadAsync(product.Id, file, default);

        var downloadResult = await uploadService.GetFileForDownloadAsync(product.Id, uploaded.Id, default);

        Assert.Equal("tai_lieu.docx", downloadResult.FileName);
        using var ms = new MemoryStream();
        await downloadResult.Stream.CopyToAsync(ms);
        Assert.Equal(bytes, ms.ToArray());
    }

    [Fact]
    public async Task Delete_ByAuthorizedUser_RemovesEvidenceAndStorageFile()
    {
        var product = await SeedProductAsync(creatorAccountId: 101);
        var service = CreateService(Lecturer(101));
        var file = CreateFormFile("temp.png", "image_bytes"u8.ToArray(), "image/png");
        var uploaded = await service.UploadAsync(product.Id, file, default);

        Assert.Single(_evidences.Items);
        Assert.Single(_storage.Files);

        await service.DeleteAsync(product.Id, uploaded.Id, default);

        Assert.Empty(_evidences.Items);
        Assert.Empty(_storage.Files);
    }

    // ---- Fakes ----

    private sealed class FakeUser(long? id, params RoleAssignmentDto[] assignments) : ICurrentUser
    {
        public bool IsAuthenticated => Id.HasValue;
        public long? Id => id;
        public string? Username => "user" + id;
        public string? FullName => "User " + id;
        public IReadOnlyList<RoleAssignmentDto> Assignments => assignments;
        public bool IsInRole(string role) => assignments.Any(a => a.Role == role);
        public bool CanAccess(long? facultyId, long? departmentId) =>
            assignments.Any(a => a.Scope switch
            {
                Scopes.Global => true,
                Scopes.Faculty => a.FacultyId == facultyId,
                Scopes.Department => a.DepartmentId == departmentId,
                _ => false,
            });
    }

    private sealed class FakeFileStorageService : IFileStorageService
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public async Task<string> SaveAsync(string subDirectory, string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            var key = $"{subDirectory}/{fileName}".Replace('\\', '/');
            using var ms = new MemoryStream();
            await content.CopyToAsync(ms, cancellationToken);
            Files[key] = ms.ToArray();
            return key;
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            if (Files.TryGetValue(storageKey, out var bytes))
                return Task.FromResult<Stream?>(new MemoryStream(bytes));
            return Task.FromResult<Stream?>(null);
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
        {
            Files.Remove(storageKey);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(Files.ContainsKey(storageKey));
    }

    private sealed class FakeProductEvidenceRepository : IProductEvidenceRepository
    {
        private long _nextId = 1;
        public List<ProductEvidence> Items { get; } = [];

        public Task<ProductEvidence?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<EvidenceResponse?> GetViewByIdAsync(long id, CancellationToken cancellationToken)
        {
            var item = Items.FirstOrDefault(x => x.Id == id);
            if (item == null) return Task.FromResult<EvidenceResponse?>(null);
            return Task.FromResult<EvidenceResponse?>(new EvidenceResponse(
                item.Id, item.ProductId, item.UploadedByAccountId, "Giảng viên A",
                item.FileName, item.MimeType, item.FileSizeBytes, item.Sha256, item.UploadedAt));
        }

        public Task<IReadOnlyList<EvidenceResponse>> GetViewsByProductIdAsync(long productId, CancellationToken cancellationToken)
        {
            var list = Items
                .Where(x => x.ProductId == productId)
                .OrderBy(x => x.UploadedAt)
                .Select(x => new EvidenceResponse(
                    x.Id, x.ProductId, x.UploadedByAccountId, "Giảng viên A",
                    x.FileName, x.MimeType, x.FileSizeBytes, x.Sha256, x.UploadedAt))
                .ToList();
            return Task.FromResult<IReadOnlyList<EvidenceResponse>>(list);
        }

        public Task<List<ProductEvidence>> GetByProductIdAsync(long productId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Where(x => x.ProductId == productId).OrderBy(x => x.UploadedAt).ToList());

        public Task AddAsync(ProductEvidence evidence, CancellationToken cancellationToken)
        {
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(evidence, _nextId++);
            Items.Add(evidence);
            return Task.CompletedTask;
        }

        public void Remove(ProductEvidence evidence) => Items.Remove(evidence);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeLecturers : ILecturerRepository
    {
        private static readonly Dictionary<long, long> FacultyOf = new() { [10] = 1, [20] = 1 };
        private readonly List<LecturerResponse> _items =
        [
            new(1, 101, "GV001", "Nguyễn Văn Một", null, null, null, null, null, null, null, 10, "Công nghệ phần mềm", 1, "CNTT"),
            new(2, 102, "GV002", "Trần Thị Hai", null, null, null, null, null, null, null, 10, "Công nghệ phần mềm", 1, "CNTT"),
        ];

        public Task<LecturerResponse?> GetViewAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<LecturerResponse?> GetViewByAccountIdAsync(long accountId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(x => x.AccountId == accountId));

        public Task<PagedResult<LecturerResponse>> SearchAsync(LecturerSearchQuery query, LecturerVisibility visibility, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Lecturer?> GetByIdAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Lecturer?> GetByAccountIdAsync(long accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ExistsByCodeAsync(string code, long? excludeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> ExistsByAccountIdAsync(long accountId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(Lecturer lecturer, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task RemoveAsync(Lecturer lecturer, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeProductTypes : IProductTypeRepository
    {
        public const long ArticleId = 1;
        private readonly List<ProductType> _items = [];

        public FakeProductTypes()
        {
            var article = new ProductType(ProductTypeCodes.Article, "Bài báo");
            typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(article, ArticleId);
            _items.Add(article);
        }

        public Task<IReadOnlyList<ProductType>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProductType>>(_items);
        public Task<ProductType?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));
        public Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task AddAsync(ProductType productType, CancellationToken cancellationToken) => throw new NotSupportedException();
        public void Remove(ProductType productType) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeProducts(FakeLecturers lecturers, FakeProductTypes types) : IProductRepository
    {
        private readonly Dictionary<long, List<AuthorSlot>> _authors = [];
        public List<Product> Items { get; } = [];

        public Task<ProductDetailResponse?> GetViewAsync(long id, CancellationToken cancellationToken) => throw new NotSupportedException();

        public async Task<IReadOnlyList<ProductAuthorResponse>> GetAuthorsAsync(long productId, CancellationToken cancellationToken)
        {
            var result = new List<ProductAuthorResponse>();
            foreach (var slot in _authors.GetValueOrDefault(productId, []).OrderBy(s => s.Order))
            {
                var l = (await lecturers.GetViewAsync(slot.LecturerId, cancellationToken))!;
                result.Add(new ProductAuthorResponse(
                    l.Id, l.AccountId, l.Code, l.FullName, slot.Role, slot.Order, slot.DepartmentId,
                    "Bộ môn " + slot.DepartmentId, l.FacultyId));
            }
            return result;
        }

        public Task<PagedResult<ProductSummaryResponse>> SearchAsync(ProductSearchQuery query, ProductVisibility visibility, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<bool> AcademicYearExistsAsync(long academicYearId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> DoiExistsAsync(string doi, long? excludeProductId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task CreateAsync(Product product, IReadOnlyList<AuthorSlot> authors, CancellationToken cancellationToken)
        {
            Items.Add(product);
            _authors[product.Id] = authors.ToList();
            return Task.CompletedTask;
        }

        public Task ReplaceAuthorsAsync(long productId, IReadOnlyList<AuthorSlot> authors, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
