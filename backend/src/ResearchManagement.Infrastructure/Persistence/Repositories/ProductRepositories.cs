using Microsoft.EntityFrameworkCore;
using Npgsql;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository(ApplicationDbContext db) : IProductRepository
{
    // Thứ tự tạm thời khi đổi chỗ tác giả: UNIQUE (san_pham_id, thu_tu_tac_gia) không cho hai dòng
    // cùng giữ một thứ tự dù chỉ trong chốc lát, nên dòng cần dời được đưa lên vùng này trước.
    private const int TemporaryOrderBase = 1000;

    private sealed class Row
    {
        public Product P { get; set; } = null!;
        public string TypeCode { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string YearCode { get; set; } = string.Empty;
        public string CreatedByName { get; set; } = string.Empty;
    }

    private sealed class AuthorRow
    {
        public long ProductId { get; set; }
        public long LecturerId { get; set; }
        public long AccountId { get; set; }
        public string LecturerCode { get; set; } = string.Empty;
        public string LecturerName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int Order { get; set; }
        public long DepartmentId { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public long FacultyId { get; set; }
    }

    private IQueryable<Row> Rows() =>
        from p in db.Products.AsNoTracking()
        join t in db.ProductTypes.AsNoTracking() on p.ProductTypeId equals t.Id
        join y in db.AcademicYears.AsNoTracking() on p.AcademicYearId equals y.Id
        join a in db.Accounts.AsNoTracking() on p.CreatedByAccountId equals a.Id
        select new Row { P = p, TypeCode = t.Code, TypeName = t.Name, YearCode = y.Code, CreatedByName = a.FullName };

    // Tác giả kèm giảng viên và bộ môn đã ghi nhận (khoa suy ra từ bộ môn đó).
    private IQueryable<AuthorRow> AuthorRows() =>
        from a in db.ProductAuthors.AsNoTracking()
        join l in db.Lecturers.AsNoTracking() on a.LecturerId equals l.Id
        join d in db.Departments.AsNoTracking() on a.DepartmentId equals d.Id
        select new AuthorRow
        {
            ProductId = a.ProductId, LecturerId = l.Id, AccountId = l.AccountId, LecturerCode = l.Code,
            LecturerName = l.FullName, Role = a.Role, Order = a.Order, DepartmentId = d.Id,
            DepartmentName = d.Name, FacultyId = d.FacultyId,
        };

    private static ProductAuthorResponse ToAuthor(AuthorRow r) => new(
        r.LecturerId, r.AccountId, r.LecturerCode, r.LecturerName, r.Role, r.Order,
        r.DepartmentId, r.DepartmentName, r.FacultyId);

    public async Task<ProductDetailResponse?> GetViewAsync(long id, CancellationToken cancellationToken)
    {
        var row = await Rows().Where(x => x.P.Id == id).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return null;

        var authors = await GetAuthorsAsync(id, cancellationToken);
        var p = row.P;
        return new ProductDetailResponse(
            p.Id, p.Code, p.Title, p.ProductTypeId, row.TypeCode, row.TypeName, p.AcademicYearId, row.YearCode,
            p.CreatedByAccountId, row.CreatedByName, p.PublicationYear, p.PublicationInfo, p.Doi, p.Isbn, p.Issn,
            p.JournalName, p.JournalIndex, p.JournalCategory, p.WorkScore, p.ResearchField, p.Publisher,
            p.ProjectLevel, p.HostUnit, p.ProjectObjective, p.ProjectContent, p.ExpectedResult,
            p.CertificateNumber, p.IssuingAuthority, p.StartDate, p.EndDate, p.SubmittedDate, p.PublishedDate,
            p.ArticleStatus, p.ReviewStatus, p.Version, authors, null);
    }

    public async Task<IReadOnlyList<ProductAuthorResponse>> GetAuthorsAsync(
        long productId, CancellationToken cancellationToken)
    {
        var rows = await AuthorRows().Where(a => a.ProductId == productId)
            .OrderBy(a => a.Order).ToListAsync(cancellationToken);
        return rows.Select(ToAuthor).ToList();
    }

    public async Task<PagedResult<ProductSummaryResponse>> SearchAsync(
        ProductSearchQuery query, ProductVisibility visibility, CancellationToken cancellationToken)
    {
        var all = visibility.All;
        var isAdmin = visibility.IsAdmin;
        var facultyIds = visibility.FacultyIds.ToArray();
        var departmentIds = visibility.DepartmentIds.ToArray();
        var me = visibility.AccountId ?? 0L;

        var authorUnits =
            from a in db.ProductAuthors.AsNoTracking()
            join d in db.Departments.AsNoTracking() on a.DepartmentId equals d.Id
            select new { a.ProductId, a.LecturerId, a.DepartmentId, d.FacultyId };
        var authorAccounts =
            from a in db.ProductAuthors.AsNoTracking()
            join l in db.Lecturers.AsNoTracking() on a.LecturerId equals l.Id
            select new { a.ProductId, l.AccountId };

        var rows = Rows();

        // Quản trị thấy tất cả. Người khác thấy sản phẩm của mình (kể cả bản nháp) và sản phẩm đã gửi duyệt
        // có tác giả thuộc phạm vi KHOA/BO_MON/TOAN_TRUONG của họ.
        if (!isAdmin)
        {
            rows = rows.Where(x =>
                x.P.CreatedByAccountId == me
                || authorAccounts.Any(o => o.ProductId == x.P.Id && o.AccountId == me)
                || (x.P.ReviewStatus != ReviewStatuses.Draft
                    && (all || authorUnits.Any(u => u.ProductId == x.P.Id
                        && (facultyIds.Contains(u.FacultyId) || departmentIds.Contains(u.DepartmentId))))));
        }

        if (query.Mine)
            rows = rows.Where(x => x.P.CreatedByAccountId == me
                || authorAccounts.Any(o => o.ProductId == x.P.Id && o.AccountId == me));
        if (query.ProductTypeId is { } typeId) rows = rows.Where(x => x.P.ProductTypeId == typeId);
        if (query.AcademicYearId is { } yearId) rows = rows.Where(x => x.P.AcademicYearId == yearId);
        if (query.ReviewStatus is { } review) rows = rows.Where(x => x.P.ReviewStatus == review);
        if (query.ArticleStatus is { } article) rows = rows.Where(x => x.P.ArticleStatus == article);
        if (query.LecturerId is { } lecturerId)
            rows = rows.Where(x => authorUnits.Any(u => u.ProductId == x.P.Id && u.LecturerId == lecturerId));
        if (query.FacultyId is { } facultyId)
            rows = rows.Where(x => authorUnits.Any(u => u.ProductId == x.P.Id && u.FacultyId == facultyId));
        if (query.DepartmentId is { } departmentId)
            rows = rows.Where(x => authorUnits.Any(u => u.ProductId == x.P.Id && u.DepartmentId == departmentId));

        if (CleanKeyword(query.Keyword) is { } keyword)
        {
            var pattern = "%" + keyword.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
            rows = rows.Where(x =>
                EF.Functions.ILike(x.P.Title, pattern) || EF.Functions.ILike(x.P.Code, pattern)
                || (x.P.JournalName != null && EF.Functions.ILike(x.P.JournalName, pattern))
                || (x.P.Doi != null && EF.Functions.ILike(x.P.Doi, pattern)));
        }

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .OrderByDescending(x => x.P.Id)
            .Skip(query.Skip).Take(query.Take)
            .ToListAsync(cancellationToken);

        // Một truy vấn lấy tác giả của cả trang, tránh N+1.
        var ids = page.Select(x => x.P.Id).ToList();
        var authorRows = new List<AuthorRow>();
        if (ids.Count > 0)
            authorRows = await AuthorRows().Where(a => ids.Contains(a.ProductId))
                .OrderBy(a => a.Order).ToListAsync(cancellationToken);
        var authorsByProduct = authorRows.ToLookup(a => a.ProductId);

        var items = page.Select(x => new ProductSummaryResponse(
            x.P.Id, x.P.Code, x.P.Title, x.P.ProductTypeId, x.TypeCode, x.TypeName, x.P.AcademicYearId, x.YearCode,
            x.P.PublicationYear, x.P.JournalName, x.P.ArticleStatus, x.P.ReviewStatus, x.P.Version,
            authorsByProduct[x.P.Id]
                .Select(a => new ProductAuthorBrief(a.LecturerId, a.LecturerName, a.Role, a.Order)).ToList())).ToList();

        return new PagedResult<ProductSummaryResponse>(items, query.Page, query.Take, total);
    }

    private static string? CleanKeyword(string? keyword)
    {
        var trimmed = keyword?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    public Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        db.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public Task<bool> AcademicYearExistsAsync(long academicYearId, CancellationToken cancellationToken) =>
        db.AcademicYears.AnyAsync(y => y.Id == academicYearId, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
        db.Products.AnyAsync(p => p.Code == code, cancellationToken);

    public Task<bool> DoiExistsAsync(string doi, long? excludeProductId, CancellationToken cancellationToken)
    {
        var lowered = doi.ToLower();
        return db.Products.AnyAsync(
            p => p.Doi != null && p.Doi.ToLower() == lowered && (excludeProductId == null || p.Id != excludeProductId),
            cancellationToken);
    }

    public async Task CreateAsync(Product product, IReadOnlyList<AuthorSlot> authors, CancellationToken cancellationToken)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await db.Products.AddAsync(product, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var a in authors)
            db.ProductAuthors.Add(new ProductAuthor(product.Id, a.LecturerId, a.DepartmentId, a.Role, a.Order));
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ReplaceAuthorsAsync(
        long productId, IReadOnlyList<AuthorSlot> authors, CancellationToken cancellationToken)
    {
        var desired = authors.ToDictionary(a => a.LecturerId);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var current = await db.ProductAuthors.Where(a => a.ProductId == productId).ToListAsync(cancellationToken);
            var kept = current.Where(a => desired.ContainsKey(a.LecturerId)).ToList();

            // Bước 1: xóa người bị bỏ và dời những dòng sắp đổi thứ tự sang vùng tạm.
            db.ProductAuthors.RemoveRange(current.Where(a => !desired.ContainsKey(a.LecturerId)));
            foreach (var row in kept.Where(a => a.Order != desired[a.LecturerId].Order))
                row.Change(row.Role, TemporaryOrderBase + desired[row.LecturerId].Order);
            await db.SaveChangesAsync(cancellationToken);

            // Bước 2: đặt vai trò và thứ tự cuối cùng, thêm người mới. Bộ môn ghi nhận của người cũ giữ nguyên.
            foreach (var row in kept)
            {
                var slot = desired[row.LecturerId];
                row.Change(slot.Role, slot.Order);
            }
            var existing = current.Select(a => a.LecturerId).ToHashSet();
            foreach (var slot in authors.Where(a => !existing.Contains(a.LecturerId)))
                db.ProductAuthors.Add(new ProductAuthor(productId, slot.LecturerId, slot.DepartmentId, slot.Role, slot.Order));
            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ConflictException(
                "Không thể bỏ tác giả này vì sản phẩm đã được tính vào kết quả đánh giá của họ.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConflictException(
                "Danh sách tác giả vừa được thay đổi ở nơi khác. Hãy tải lại trang và thử lại.");
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}

public sealed class ProductEvidenceRepository(ApplicationDbContext db) : IProductEvidenceRepository
{
    public Task<ProductEvidence?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        db.ProductEvidences.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<EvidenceResponse?> GetViewByIdAsync(long id, CancellationToken cancellationToken) =>
        await (from e in db.ProductEvidences.AsNoTracking()
               join a in db.Accounts.AsNoTracking() on e.UploadedByAccountId equals a.Id
               where e.Id == id
               select new EvidenceResponse(
                   e.Id,
                   e.ProductId,
                   e.UploadedByAccountId,
                   a.FullName,
                   e.FileName,
                   e.MimeType,
                   e.FileSizeBytes,
                   e.Sha256,
                   e.UploadedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<EvidenceResponse>> GetViewsByProductIdAsync(long productId, CancellationToken cancellationToken) =>
        await (from e in db.ProductEvidences.AsNoTracking()
               join a in db.Accounts.AsNoTracking() on e.UploadedByAccountId equals a.Id
               where e.ProductId == productId
               orderby e.UploadedAt
               select new EvidenceResponse(
                   e.Id,
                   e.ProductId,
                   e.UploadedByAccountId,
                   a.FullName,
                   e.FileName,
                   e.MimeType,
                   e.FileSizeBytes,
                   e.Sha256,
                   e.UploadedAt))
            .ToListAsync(cancellationToken);

    public Task<List<ProductEvidence>> GetByProductIdAsync(long productId, CancellationToken cancellationToken) =>
        db.ProductEvidences
            .Where(e => e.ProductId == productId)
            .OrderBy(e => e.UploadedAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ProductEvidence evidence, CancellationToken cancellationToken) =>
        await db.ProductEvidences.AddAsync(evidence, cancellationToken);

    public void Remove(ProductEvidence evidence) =>
        db.ProductEvidences.Remove(evidence);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}

public sealed class ReviewHistoryRepository(ApplicationDbContext db) : IReviewHistoryRepository
{
    public Task<List<ReviewHistory>> GetByProductIdAsync(long productId, CancellationToken cancellationToken) =>
        db.ReviewHistories
            .Where(h => h.ProductId == productId)
            .OrderBy(h => h.OccurredAt)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(ReviewHistory history, CancellationToken cancellationToken) =>
        await db.ReviewHistories.AddAsync(history, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);
}
