using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Lecturers;

public sealed class ScientificProfileService(
    ILecturerRepository lecturers,
    IScientificProfileRepository profiles,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<ScientificProfileResponse> GetAsync(long lecturerId, CancellationToken cancellationToken)
    {
        var lecturer = await FindLecturerAsync(lecturerId, cancellationToken);
        if (!LecturerAccess.CanRead(currentUser, lecturer))
            throw new ForbiddenException("Không có quyền xem lý lịch khoa học của giảng viên này.");

        var profile = await profiles.GetByLecturerIdAsync(lecturerId, cancellationToken);
        return profile is null ? Empty(lecturerId) : ToResponse(profile);
    }

    /// <summary>Tạo mới hoặc ghi đè toàn bộ lý lịch. Trường bỏ trống được lưu là rỗng.</summary>
    public async Task<ScientificProfileResponse> UpsertAsync(
        long lecturerId, ScientificProfileRequest request, CancellationToken cancellationToken)
    {
        var lecturer = await FindLecturerAsync(lecturerId, cancellationToken);
        if (!LecturerAccess.CanEditProfile(currentUser, lecturer))
            throw new ForbiddenException("Không có quyền sửa lý lịch khoa học của giảng viên này.");

        var now = clock.GetUtcNow();
        var expertise = TextNormalizer.Clean(request.Expertise);
        var fields = TextNormalizer.Clean(request.ResearchFields);
        var directions = TextNormalizer.Clean(request.ResearchDirections);
        var summary = TextNormalizer.Clean(request.ActivitySummary);

        var profile = await profiles.GetByLecturerIdAsync(lecturerId, cancellationToken);
        if (profile is null)
        {
            profile = new ScientificProfile(lecturerId, expertise, fields, directions, summary, now);
            await profiles.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.Update(expertise, fields, directions, summary, now);
        }

        await profiles.SaveChangesAsync(cancellationToken);
        return ToResponse(profile);
    }

    public async Task<ScientificProfileResponse> GetMineAsync(CancellationToken cancellationToken) =>
        await GetAsync(await MyLecturerIdAsync(cancellationToken), cancellationToken);

    public async Task<ScientificProfileResponse> UpsertMineAsync(
        ScientificProfileRequest request, CancellationToken cancellationToken) =>
        await UpsertAsync(await MyLecturerIdAsync(cancellationToken), request, cancellationToken);

    private async Task<long> MyLecturerIdAsync(CancellationToken cancellationToken)
    {
        var accountId = currentUser.Id ?? throw new ForbiddenException("Chưa đăng nhập.");
        var lecturer = await lecturers.GetViewByAccountIdAsync(accountId, cancellationToken)
            ?? throw new NotFoundException("Tài khoản chưa được liên kết với hồ sơ giảng viên.");
        return lecturer.Id;
    }

    private async Task<LecturerResponse> FindLecturerAsync(long lecturerId, CancellationToken cancellationToken) =>
        await lecturers.GetViewAsync(lecturerId, cancellationToken)
            ?? throw new NotFoundException("Không tìm thấy giảng viên.");

    private static ScientificProfileResponse Empty(long lecturerId) =>
        new(lecturerId, null, null, null, null, null);

    private static ScientificProfileResponse ToResponse(ScientificProfile p) =>
        new(p.LecturerId, p.Expertise, p.ResearchFields, p.ResearchDirections, p.ActivitySummary, p.UpdatedAt);
}
