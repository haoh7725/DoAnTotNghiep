using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Infrastructure;
using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Products;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController, Authorize, Route("api")]
public sealed class ArticleProgressRequestsController(
    ApplicationDbContext db,
    ICurrentUser currentUser,
    WorkflowNotificationDispatcher notifications) : ControllerBase
{
    public sealed record CreateRequest(
        string ToStatus,
        long EvidenceId,
        DateOnly? PublishedDate,
        string? PublicationInfo,
        string? Note);

    public sealed record ReviewRequest(string Action, string? Comment);
    public sealed record ProgressResponse(long Id, long ProductId, string ProductTitle,
        string FromStatus, string ToStatus, long EvidenceId, string EvidenceName,
        string RequiredRole, DateOnly? PublishedDate, string? PublicationInfo, string? Note,
        string Status, long RequestedByAccountId, string RequesterName, DateTimeOffset RequestedAt,
        long? ReviewedByAccountId, string? ReviewerName, string? ReviewComment, DateTimeOffset? ReviewedAt);

    [HttpGet("products/{productId:long}/article-progress-requests")]
    public async Task<IActionResult> List(long productId, CancellationToken ct)
    {
        var context = await GetProductContext(productId, ct);
        await EnsureCanRead(context, ct);
        var rows = await Query(productId: productId).ToListAsync(ct);
        return Ok(rows.OrderByDescending(x => x.RequestedAt));
    }

    [HttpGet("article-progress-requests/pending")]
    [Authorize(Roles = "TRUONG_BO_MON,TRUONG_KHOA,QUAN_TRI")]
    public async Task<IActionResult> Pending(CancellationToken ct)
    {
        var rows = await Query(status: "CHO_DUYET").ToListAsync(ct);
        var visible = new List<object>();
        foreach (var row in rows.OrderBy(x => x.RequestedAt))
        {
            var context = await GetProductContext(row.ProductId, ct);
            if (CanReview(context, row.RequiredRole)) visible.Add(row);
        }
        return Ok(visible);
    }

    [HttpPost("products/{productId:long}/article-progress-requests")]
    public async Task<IActionResult> Create(long productId, CreateRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var context = await GetProductContext(productId, ct, tracking: true);
        if (context.OwnerAccountId != UserId())
            throw new ForbiddenException("Chỉ tác giả chính được đề nghị cập nhật trạng thái bài báo.");
        if (context.ProductTypeCode != "BAI_BAO")
            throw new BusinessRuleException("Chỉ bài báo có luồng đề nghị cập nhật trạng thái công bố.");
        if (context.Product.Status == ProductStatuses.Approved)
            throw new BusinessRuleException("Bài báo đã được công nhận hoàn tất.");
        if (await db.ArticleProgressRequests.AnyAsync(x => x.ProductId == productId && x.Status == "CHO_DUYET", ct))
            throw new ConflictException("Bài báo đang có một đề nghị chờ xét duyệt.");

        var fromStatus = PublicStatus(context.Product.ArticleStatus);
        var toStatus = NormalizeRequestedStatus(request.ToStatus);
        ArticleProgressPolicy.ValidateTransition(fromStatus, toStatus, 1);
        if (toStatus != ArticleProgressRequestPolicy.Next(fromStatus))
            throw new BusinessRuleException("Đề nghị phải chuyển đúng một bước tiếp theo.");
        var evidence = await db.ProductEvidences.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.EvidenceId && x.ProductId == productId, ct)
            ?? throw new BusinessRuleException("Minh chứng không thuộc bài báo này.");
        if (toStatus == "DA_XUAT_BAN" && !request.PublishedDate.HasValue)
            throw new BusinessRuleException("Đề nghị xuất bản phải có ngày công bố.");
        if (toStatus != "DA_XUAT_BAN" && request.PublishedDate.HasValue)
            throw new BusinessRuleException("Chỉ nhập ngày công bố ở bước Đã xuất bản.");
        if (request.PublishedDate.HasValue && request.PublishedDate > Today())
            throw new BusinessRuleException("Ngày công bố không được nằm trong tương lai.");
        if (toStatus == "DA_XUAT_BAN" && string.IsNullOrWhiteSpace(request.PublicationInfo ?? context.Product.PublicationInfo))
            throw new BusinessRuleException("Đề nghị xuất bản phải có nơi công bố.");
        if (request.Note?.Length > 1000 || request.PublicationInfo?.Length > 500)
            throw new BusinessRuleException("Nội dung đề nghị vượt quá độ dài cho phép.");

        var requiredRole = ArticleProgressRequestPolicy.RequiredRole(toStatus);
        var entity = new ArticleProgressRequest(productId, UserId(), fromStatus, toStatus,
            evidence.Id, requiredRole, request.PublishedDate,
            request.PublicationInfo ?? context.Product.PublicationInfo, request.Note);
        db.ArticleProgressRequests.Add(entity);
        await db.SaveChangesAsync(ct);
        await notifications.QueueForRoleAsync(requiredRole,
            $"Có đề nghị chuyển tiến độ bài báo '{context.Product.Title}' sang {StatusLabel(toStatus)}.",
            $"ARTICLE_PROGRESS_REQUEST:{entity.Id}", $"products:{productId}", context.FacultyId,
            requiredRole == Roles.DepartmentHead ? context.DepartmentId : null, productId, ct, UserId());
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Created($"/api/article-progress-requests/{entity.Id}", await Query(id: entity.Id).SingleAsync(ct));
    }

    [HttpPost("article-progress-requests/{id:long}/review")]
    [Authorize(Roles = "TRUONG_BO_MON,TRUONG_KHOA,QUAN_TRI")]
    public async Task<IActionResult> Review(long id, ReviewRequest request, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var entity = await db.ArticleProgressRequests.SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new NotFoundException("Không tìm thấy đề nghị cập nhật tiến độ.");
        if (entity.Status != "CHO_DUYET") throw new ConflictException("Đề nghị đã được xử lý.");
        var context = await GetProductContext(entity.ProductId, ct, tracking: true);
        if (!CanReview(context, entity.RequiredRole))
            throw new ForbiddenException("Không có quyền xét đề nghị này trong đơn vị được phân công.");
        if (await IsAuthor(entity.ProductId, UserId(), ct))
            throw new ForbiddenException("Không được tự xác nhận tiến độ bài báo mình tham gia.");

        var action = request.Action.Trim().ToUpperInvariant();
        if (action == "APPROVE")
        {
            if (PublicStatus(context.Product.ArticleStatus) != entity.FromStatus)
                throw new ConflictException("Trạng thái bài báo đã thay đổi; đề nghị này không còn hợp lệ.");
            if (!await db.ProductEvidences.AnyAsync(x => x.Id == entity.EvidenceId && x.ProductId == entity.ProductId, ct))
                throw new BusinessRuleException("Minh chứng của đề nghị không còn tồn tại.");
            ArticleProgressPolicy.ValidateTransition(entity.FromStatus, entity.ToStatus, 1);
            if (entity.PublishedDate.HasValue && entity.PublishedDate > Today())
                throw new BusinessRuleException("Ngày công bố không được nằm trong tương lai.");
            context.Product.UpdateArticleProgress(StoredStatus(entity.ToStatus), entity.PublishedDate,
                entity.PublicationInfo ?? context.Product.PublicationInfo);
            db.Products.Update(context.Product);
            entity.Approve(UserId(), request.Comment);
            db.ReviewHistories.Add(new ReviewHistory(context.Product.Id, UserId(), context.Product.Status,
                context.Product.Status, $"Xác nhận đề nghị tiến độ: {StatusLabel(entity.FromStatus)} → {StatusLabel(entity.ToStatus)}. {request.Comment}"));
        }
        else if (action == "REJECT")
        {
            try { entity.Reject(UserId(), request.Comment ?? string.Empty); }
            catch (InvalidOperationException ex) { throw new BusinessRuleException(ex.Message); }
        }
        else throw new BusinessRuleException("Hành động phải là APPROVE hoặc REJECT.");

        var notification = new Notification(context.OwnerAccountId,
            action == "APPROVE" ? "HE_THONG" : "PHAN_HOI",
            action == "APPROVE"
                ? $"Đề nghị tiến độ '{context.Product.Title}' đã được xác nhận: {StatusLabel(entity.ToStatus)}."
                : $"Đề nghị tiến độ '{context.Product.Title}' bị trả lại: {entity.ReviewComment}",
            $"ARTICLE_PROGRESS_REVIEWED:{entity.Id}:{entity.Status}", scientificProductId: entity.ProductId,
            actionPath: $"products:{entity.ProductId}");
        notification.MarkAsSent();
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Ok(await Query(id: id).SingleAsync(ct));
    }

    private IQueryable<ProgressResponse> Query(long? id = null, long? productId = null, string? status = null)
    {
        var requests = db.ArticleProgressRequests.AsNoTracking().AsQueryable();
        if (id.HasValue) requests = requests.Where(x => x.Id == id.Value);
        if (productId.HasValue) requests = requests.Where(x => x.ProductId == productId.Value);
        if (status is not null) requests = requests.Where(x => x.Status == status);
        return
        from request in requests
        join product in db.Products.AsNoTracking() on request.ProductId equals product.Id
        join requester in db.Accounts.AsNoTracking() on request.RequestedByAccountId equals requester.Id
        join evidence in db.ProductEvidences.AsNoTracking() on request.EvidenceId equals evidence.Id
        join reviewer in db.Accounts.AsNoTracking() on request.ReviewedByAccountId equals reviewer.Id into reviewers
        from reviewer in reviewers.DefaultIfEmpty()
        select new ProgressResponse(
            request.Id, request.ProductId, product.Title, request.FromStatus, request.ToStatus,
            request.EvidenceId, evidence.OriginalFileName, request.RequiredRole,
            request.PublishedDate, request.PublicationInfo, request.Note, request.Status,
            request.RequestedByAccountId, requester.FullName, request.RequestedAt,
            request.ReviewedByAccountId, reviewer == null ? null : reviewer.FullName,
            request.ReviewComment, request.ReviewedAt);
    }

    private sealed record ProductContext(Product Product, string ProductTypeCode, long OwnerAccountId,
        long FacultyId, long DepartmentId);

    private async Task<ProductContext> GetProductContext(long productId, CancellationToken ct, bool tracking = false)
    {
        var products = tracking ? db.Products.AsQueryable() : db.Products.AsNoTracking();
        return await (from product in products
            join item in db.ResearchPlanItems.AsNoTracking() on product.ResearchPlanItemId equals item.Id
            join type in db.ProductTypes.AsNoTracking() on item.ProductTypeId equals type.Id
            join lecturer in db.Lecturers.AsNoTracking() on product.SubmittedByLecturerId equals lecturer.Id
            join department in db.Departments.AsNoTracking() on lecturer.DepartmentId equals department.Id
            where product.Id == productId
            select new ProductContext(product, type.Code, lecturer.AccountId, department.FacultyId, department.Id))
            .SingleOrDefaultAsync(ct) ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
    }

    private async Task EnsureCanRead(ProductContext context, CancellationToken ct)
    {
        if (context.OwnerAccountId == UserId() || currentUser.CanAccess(context.FacultyId, context.DepartmentId) ||
            await IsAuthor(context.Product.Id, UserId(), ct)) return;
        throw new ForbiddenException("Không có quyền xem đề nghị của sản phẩm này.");
    }

    private bool CanReview(ProductContext context, string role) => currentUser.IsInRole(Roles.Admin) ||
        currentUser.Assignments.Any(a => a.Role == role && ScopeRules.CanAccess([a], context.FacultyId, context.DepartmentId));

    private async Task<bool> IsAuthor(long productId, long accountId, CancellationToken ct) =>
        await db.Products.AnyAsync(x => x.Id == productId && db.Lecturers.Any(l => l.Id == x.SubmittedByLecturerId && l.AccountId == accountId), ct) ||
        await db.ProductCoAuthors.AnyAsync(a => a.ProductId == productId && db.Lecturers.Any(l => l.Id == a.LecturerId && l.AccountId == accountId), ct);

    private long UserId() => currentUser.Id ?? throw new ForbiddenException("Không xác định được tài khoản.");
    private static DateOnly Today() => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow,
        OperatingSystem.IsWindows() ? "SE Asia Standard Time" : "Asia/Ho_Chi_Minh").DateTime);
    private static string PublicStatus(string? status) => status == "DA_NHAN_DANG" ? "DA_NHAN_XET" : status ?? "DANG_VIET";
    private static string StoredStatus(string status) => status == "DA_NHAN_XET" ? "DA_NHAN_DANG" : status;
    private static string NormalizeRequestedStatus(string value) => value.Trim().ToUpperInvariant() switch
    {
        "DANG_PHAN_BIEN" => "DANG_PHAN_BIEN",
        "DA_NHAN_XET" or "DA_NHAN_DANG" => "DA_NHAN_XET",
        "DA_XUAT_BAN" => "DA_XUAT_BAN",
        _ => throw new BusinessRuleException("Trạng thái đề nghị không hợp lệ.")
    };
    private static string StatusLabel(string value) => value switch
    {
        "DANG_VIET" => "Đang viết", "DANG_PHAN_BIEN" => "Đang phản biện",
        "DA_NHAN_XET" => "Đã nhận xét", "DA_XUAT_BAN" => "Đã xuất bản", _ => value
    };
}
