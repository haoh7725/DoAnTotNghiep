namespace ResearchManagement.Application.ResearchPlans.Models;

public sealed record LecturerAccessInfo(
    long LecturerId,
    long AccountId,
    long DepartmentId,
    long FacultyId);