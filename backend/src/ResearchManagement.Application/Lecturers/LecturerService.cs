using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Lecturers;

public sealed class LecturerService(
    ILecturerRepository lecturers,
    IAccountRepository accounts,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public Task<PagedResult<LecturerResponse>> SearchAsync(
        LecturerSearchQuery query, CancellationToken cancellationToken) =>
        lecturers.SearchAsync(query, LecturerAccess.VisibilityOf(currentUser), cancellationToken);

    public async Task<LecturerResponse> GetByIdAsync(long id, CancellationToken cancellationToken)
    {
        var lecturer = await lecturers.GetViewAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");

        if (!LecturerAccess.CanRead(currentUser, lecturer))
            throw new ForbiddenException("Không có quyền xem hồ sơ giảng viên này.");

        return lecturer;
    }

    /// <summary>Hồ sơ giảng viên gắn với tài khoản đang đăng nhập.</summary>
    public async Task<LecturerResponse> GetMineAsync(CancellationToken cancellationToken) =>
        await lecturers.GetViewByAccountIdAsync(RequireAccountId(), cancellationToken)
            ?? throw new NotFoundException("Tài khoản chưa được liên kết với hồ sơ giảng viên.");

    public async Task<LecturerResponse> CreateAsync(
        CreateLecturerRequest request, CancellationToken cancellationToken)
    {
        RequireOffice();

        var code = request.Code.Trim();
        var birthDate = ValidBirthDate(request.BirthDate);

        if (await accounts.GetByIdAsync(request.AccountId, cancellationToken) is null)
            throw new BusinessRuleException("Tài khoản không tồn tại.");
        if (!await lecturers.DepartmentExistsAsync(request.DepartmentId, cancellationToken))
            throw new BusinessRuleException("Bộ môn không tồn tại.");
        if (await lecturers.ExistsByAccountIdAsync(request.AccountId, cancellationToken))
            throw new ConflictException("Tài khoản đã được liên kết với một giảng viên khác.");
        if (await lecturers.ExistsByCodeAsync(code, null, cancellationToken))
            throw new ConflictException("Mã giảng viên đã tồn tại.");

        var lecturer = new Lecturer(
            request.DepartmentId, request.AccountId, code, request.FullName.Trim(),
            birthDate, TextNormalizer.Clean(request.Gender), TextNormalizer.Clean(request.Email),
            TextNormalizer.Clean(request.Phone), TextNormalizer.Clean(request.AcademicRank),
            TextNormalizer.Clean(request.Degree), TextNormalizer.Clean(request.Position));

        await lecturers.AddAsync(lecturer, cancellationToken);
        await lecturers.SaveChangesAsync(cancellationToken);

        return await GetViewAfterSaveAsync(lecturer.Id, cancellationToken);
    }

    /// <summary>Phòng QLKH/Quản trị sửa toàn bộ thông tin. Tài khoản liên kết không đổi được.</summary>
    public async Task<LecturerResponse> UpdateAsync(
        long id, UpdateLecturerRequest request, CancellationToken cancellationToken)
    {
        RequireOffice();

        var lecturer = await lecturers.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");

        var code = request.Code.Trim();
        var birthDate = ValidBirthDate(request.BirthDate);

        if (request.DepartmentId != lecturer.DepartmentId &&
            !await lecturers.DepartmentExistsAsync(request.DepartmentId, cancellationToken))
            throw new BusinessRuleException("Bộ môn không tồn tại.");
        if (await lecturers.ExistsByCodeAsync(code, id, cancellationToken))
            throw new ConflictException("Mã giảng viên đã tồn tại.");

        lecturer.Update(
            request.DepartmentId, code, request.FullName.Trim(), birthDate,
            TextNormalizer.Clean(request.Gender), TextNormalizer.Clean(request.Email),
            TextNormalizer.Clean(request.Phone), TextNormalizer.Clean(request.AcademicRank),
            TextNormalizer.Clean(request.Degree), TextNormalizer.Clean(request.Position));

        await lecturers.SaveChangesAsync(cancellationToken);

        return await GetViewAfterSaveAsync(id, cancellationToken);
    }

    /// <summary>
    /// Giảng viên tự sửa thông tin cá nhân. Bộ môn, mã, học hàm, học vị và chức vụ do Phòng QLKH
    /// quản lý vì thống kê và đánh giá dựa trên các trường này.
    /// </summary>
    public async Task<LecturerResponse> UpdateMineAsync(
        UpdateMyLecturerRequest request, CancellationToken cancellationToken)
    {
        var lecturer = await lecturers.GetByAccountIdAsync(RequireAccountId(), cancellationToken)
            ?? throw new NotFoundException("Tài khoản chưa được liên kết với hồ sơ giảng viên.");

        lecturer.UpdatePersonal(
            request.FullName.Trim(), ValidBirthDate(request.BirthDate),
            TextNormalizer.Clean(request.Gender), TextNormalizer.Clean(request.Email),
            TextNormalizer.Clean(request.Phone));

        await lecturers.SaveChangesAsync(cancellationToken);

        return await GetViewAfterSaveAsync(lecturer.Id, cancellationToken);
    }

    /// <summary>
    /// Chỉ Quản trị được xóa. Giảng viên đã có kế hoạch, sản phẩm hoặc đánh giá sẽ bị khóa ngoại
    /// từ chối và API trả 409.
    /// </summary>
    public async Task DeleteAsync(long id, CancellationToken cancellationToken)
    {
        if (!currentUser.IsInRole(Roles.Admin))
            throw new ForbiddenException("Chỉ quản trị viên được xóa hồ sơ giảng viên.");

        var lecturer = await lecturers.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");

        await lecturers.RemoveAsync(lecturer, cancellationToken);
        await lecturers.SaveChangesAsync(cancellationToken);
    }

    private long RequireAccountId() =>
        currentUser.Id ?? throw new ForbiddenException("Chưa đăng nhập.");

    private void RequireOffice()
    {
        if (!LecturerAccess.IsOffice(currentUser))
            throw new ForbiddenException("Chỉ Phòng QLKH hoặc quản trị viên được thực hiện thao tác này.");
    }

    private DateOnly? ValidBirthDate(DateOnly? birthDate)
    {
        if (birthDate is { } date &&
            (date.Year < 1900 || date > DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime)))
            throw new BusinessRuleException("Ngày sinh không hợp lệ.");
        return birthDate;
    }

    private async Task<LecturerResponse> GetViewAfterSaveAsync(long id, CancellationToken cancellationToken) =>
        await lecturers.GetViewAsync(id, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");
}
