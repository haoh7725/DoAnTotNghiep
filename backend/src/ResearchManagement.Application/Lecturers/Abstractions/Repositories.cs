using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Application.Lecturers.Abstractions;

public interface ILecturerRepository
{
    // Đọc: trả về bản ghi đã nối bộ môn và khoa.
    Task<LecturerResponse?> GetViewAsync(long id, CancellationToken cancellationToken);
    Task<LecturerResponse?> GetViewByAccountIdAsync(long accountId, CancellationToken cancellationToken);
    Task<PagedResult<LecturerResponse>> SearchAsync(
        LecturerSearchQuery query, LecturerVisibility visibility, CancellationToken cancellationToken);

    // Ghi: trả về entity được theo dõi.
    Task<Lecturer?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<Lecturer?> GetByAccountIdAsync(long accountId, CancellationToken cancellationToken);

    Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken);
    Task<bool> ExistsByCodeAsync(string code, long? excludeId, CancellationToken cancellationToken);
    Task<bool> ExistsByAccountIdAsync(long accountId, CancellationToken cancellationToken);

    Task AddAsync(Lecturer lecturer, CancellationToken cancellationToken);

    /// <summary>Xóa giảng viên cùng lý lịch khoa học trong cùng một lần lưu.</summary>
    Task RemoveAsync(Lecturer lecturer, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface IScientificProfileRepository
{
    Task<ScientificProfile?> GetByLecturerIdAsync(long lecturerId, CancellationToken cancellationToken);
    Task AddAsync(ScientificProfile profile, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
