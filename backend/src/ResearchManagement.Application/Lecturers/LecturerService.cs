using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
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

    public Task<IReadOnlyList<LecturerAccountOptionResponse>> GetAvailableAccountOptionsAsync(
        CancellationToken cancellationToken)
    {
        RequireOffice();
        return accounts.GetAvailableLecturerAccountsAsync(cancellationToken);
    }

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

        var account = await accounts.GetByIdAsync(request.AccountId, cancellationToken);
        if (account is null)
            throw new BusinessRuleException("Tài khoản không tồn tại.");
        if (!account.IsActive)
            throw new BusinessRuleException("Chỉ được liên kết tài khoản đang hoạt động.");

        var eligibleRoles = new[] { Roles.Lecturer, Roles.DepartmentHead, Roles.FacultyDean };
        var eligibleAssignments = account.RoleAssignments
            .Where(x => eligibleRoles.Contains(x.Role))
            .ToList();
        if (eligibleAssignments.Count == 0)
            throw new BusinessRuleException("Tài khoản phải có vai trò Giảng viên, Trưởng bộ môn hoặc Trưởng khoa.");
        if (account.RoleAssignments.Any(x => x.Role is Roles.ResearchOffice or Roles.BoardOfDirectors or Roles.Admin))
            throw new BusinessRuleException("Tài khoản quản trị toàn trường không được liên kết hồ sơ giảng viên.");

        var departmentFacultyId = await lecturers.GetDepartmentFacultyIdAsync(request.DepartmentId, cancellationToken);
        if (!departmentFacultyId.HasValue)
            throw new BusinessRuleException("Bộ môn không tồn tại.");

        var scopedDepartmentIds = eligibleAssignments
            .Where(x => x.Role == Roles.DepartmentHead && x.DepartmentId.HasValue)
            .Select(x => x.DepartmentId!.Value)
            .Distinct()
            .ToList();
        if (scopedDepartmentIds.Count > 0 && !scopedDepartmentIds.Contains(request.DepartmentId))
            throw new BusinessRuleException("Bộ môn của hồ sơ phải khớp với phạm vi Trưởng bộ môn đã được phân quyền.");

        var scopedFacultyIds = eligibleAssignments
            .Where(x => x.Role == Roles.FacultyDean && x.FacultyId.HasValue)
            .Select(x => x.FacultyId!.Value)
            .Distinct()
            .ToList();
        if (scopedFacultyIds.Count > 0 && !scopedFacultyIds.Contains(departmentFacultyId.Value))
            throw new BusinessRuleException("Bộ môn của hồ sơ phải thuộc khoa mà Trưởng khoa được phân quyền.");
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
