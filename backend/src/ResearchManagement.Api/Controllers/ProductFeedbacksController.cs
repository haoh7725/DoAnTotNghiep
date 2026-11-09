using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResearchManagement.Api.Authorization;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Controllers;

[ApiController, Authorize, Route("api/products/{productId:long}/feedback")]
public sealed class ProductFeedbacksController(ApplicationDbContext db, ICurrentUser user) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(long productId, CancellationToken ct)
    {
        await EnsureAccess(productId, ct);
        return Ok(await (from f in db.ProductFeedbacks.AsNoTracking()
                         join a in db.Accounts.AsNoTracking() on f.SenderAccountId equals a.Id
                         where f.ProductId == productId orderby f.CreatedAt
                         select new { f.Id, f.ProductId, f.SenderAccountId, SenderName = a.Username, f.Content, f.CreatedAt }).ToListAsync(ct));
    }

    public sealed record CreateFeedbackRequest(string? Content);
    [HttpPost, Authorize(Policy = Policies.ResearchOffice)]
    public async Task<IActionResult> Create(long productId, CreateFeedbackRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Content)) throw new BusinessRuleException("Nội dung phản hồi không được trống.");
        if (request.Content.Trim().Length > 2000) throw new BusinessRuleException("Nội dung phản hồi tối đa 2000 ký tự.");
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId, ct) ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        var owner = await db.Lecturers.SingleAsync(x => x.Id == product.SubmittedByLecturerId, ct);
        var feedback = new ProductFeedback(productId, user.Id!.Value, request.Content);
        db.ProductFeedbacks.Add(feedback); await db.SaveChangesAsync(ct);
        var notification = new Notification(owner.AccountId, "PHAN_HOI", $"Phòng QLKH có phản hồi mới cho sản phẩm '{product.Title}'.", $"PHAN_HOI:{feedback.Id}", feedbackId: feedback.Id, scientificProductId: productId);
        notification.MarkAsSent(); db.Notifications.Add(notification); await db.SaveChangesAsync(ct);
        var senderName = await db.Accounts.Where(x => x.Id == user.Id.Value).Select(x => x.Username).SingleAsync(ct);
        return CreatedAtAction(nameof(Get), new { productId }, new { feedback.Id, feedback.ProductId, feedback.SenderAccountId, SenderName = senderName, feedback.Content, feedback.CreatedAt });
    }

    private async Task EnsureAccess(long productId, CancellationToken ct)
    {
        var data = await (from p in db.Products.AsNoTracking() join l in db.Lecturers.AsNoTracking() on p.SubmittedByLecturerId equals l.Id join d in db.Departments.AsNoTracking() on l.DepartmentId equals d.Id where p.Id == productId select new { l.AccountId, d.FacultyId, DepartmentId = d.Id }).SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException("Không tìm thấy sản phẩm.");
        if (data.AccountId != user.Id && !user.CanAccess(data.FacultyId, data.DepartmentId)) throw new ForbiddenException("Không có quyền xem phản hồi.");
    }
}
