using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Lecturers;
using ResearchManagement.Application.Lecturers.Abstractions;
using ResearchManagement.Application.Lecturers.Models;
using ResearchManagement.Domain.Common;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Tests;

public class LecturerServiceTests
{
    // Dữ liệu nền: bộ môn 10 và 20 thuộc khoa 1; bộ môn 30 thuộc khoa 2.
    private readonly FakeLecturerRepository _lecturers = new();
    private readonly FakeProfileRepository _profiles = new();
    private readonly FakeAccounts _accounts = new(100, 101, 102, 103);

    private static FakeUser Office() => new(1, new RoleAssignmentDto(Roles.ResearchOffice, Scopes.Global, null, null));
    private static FakeUser Admin() => new(2, new RoleAssignmentDto(Roles.Admin, Scopes.Global, null, null));
    private static FakeUser Board() => new(3, new RoleAssignmentDto(Roles.BoardOfDirectors, Scopes.Global, null, null));
    private static FakeUser Head(long faculty, long department) =>
        new(4, new RoleAssignmentDto(Roles.DepartmentHead, Scopes.Department, faculty, department));
    private static FakeUser Dean(long faculty) => new(5, new RoleAssignmentDto(Roles.FacultyDean, Scopes.Faculty, faculty, null));
    private static FakeUser Owner(long accountId) => new(accountId, new RoleAssignmentDto(Roles.Lecturer, Scopes.Personal, null, null));

    private LecturerService Lecturers(ICurrentUser user) =>
        new(_lecturers, _accounts, user, TimeProvider.System);

    private ScientificProfileService Profiles(ICurrentUser user) =>
        new(_lecturers, _profiles, user, TimeProvider.System);

    private Lecturer Seed(long accountId, long departmentId, string code)
    {
        var lecturer = new Lecturer(departmentId, accountId, code, "Giảng viên " + code,
            null, null, null, null, "PGS", "Tiến sĩ", null);
        _lecturers.Add(lecturer);
        return lecturer;
    }

    private static CreateLecturerRequest NewRequest(long accountId = 100, long departmentId = 10, string code = "GV001") => new()
    {
        AccountId = accountId, DepartmentId = departmentId, Code = code, FullName = "  Nguyễn Văn A  "
    };

    // ---- Tạo, sửa, xóa ----

    [Fact]
    public async Task AccountOptions_OnlyResearchOfficeOrAdminCanRead()
    {
        Assert.Equal(4, (await Lecturers(Office()).GetAvailableAccountOptionsAsync(default)).Count);
        Assert.Equal(4, (await Lecturers(Admin()).GetAvailableAccountOptionsAsync(default)).Count);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Lecturers(Dean(1)).GetAvailableAccountOptionsAsync(default));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Lecturers(Board()).GetAvailableAccountOptionsAsync(default));
    }

    [Fact]
    public async Task Create_ByOffice_ReturnsDepartmentAndFacultyFromBaseData()
    {
        var result = await Lecturers(Office()).CreateAsync(NewRequest(departmentId: 30), default);

        Assert.Equal("Nguyễn Văn A", result.FullName);
        Assert.Equal(30, result.DepartmentId);
        Assert.Equal(2, result.FacultyId);
        Assert.True(result.Id > 0);
    }

    [Fact]
    public async Task Create_ByLecturerOrDepartmentHead_IsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => Lecturers(Owner(100)).CreateAsync(NewRequest(), default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Lecturers(Head(1, 10)).CreateAsync(NewRequest(), default));
    }

    [Fact]
    public async Task Create_RejectsDuplicateCodeAndAlreadyLinkedAccount()
    {
        Seed(100, 10, "GV001");

        await Assert.ThrowsAsync<ConflictException>(() =>
            Lecturers(Office()).CreateAsync(NewRequest(accountId: 101, code: "GV001"), default));
        await Assert.ThrowsAsync<ConflictException>(() =>
            Lecturers(Office()).CreateAsync(NewRequest(accountId: 100, code: "GV002"), default));
    }

    [Fact]
    public async Task Create_RejectsUnknownAccountDepartmentAndFutureBirthDate()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Lecturers(Office()).CreateAsync(NewRequest(accountId: 999), default));
        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            Lecturers(Office()).CreateAsync(NewRequest(departmentId: 999), default));

        var future = NewRequest();
        future.BirthDate = new DateOnly(2999, 1, 1);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Lecturers(Office()).CreateAsync(future, default));
    }

    [Fact]
    public async Task Create_StoresBlankOptionalFieldsAsNull()
    {
        var request = NewRequest();
        request.Email = "   ";
        request.Position = "";

        var result = await Lecturers(Office()).CreateAsync(request, default);

        Assert.Null(result.Email);
        Assert.Null(result.Position);
    }

    [Fact]
    public async Task Update_ByOffice_MovesDepartmentAndKeepsOwnCode()
    {
        var lecturer = Seed(100, 10, "GV001");
        var request = new UpdateLecturerRequest { DepartmentId = 30, Code = "GV001", FullName = "Tên mới", Degree = "Thạc sĩ" };

        var result = await Lecturers(Office()).UpdateAsync(lecturer.Id, request, default);

        Assert.Equal(30, result.DepartmentId);
        Assert.Equal(2, result.FacultyId);
        Assert.Equal("Thạc sĩ", result.Degree);
        Assert.Null(result.AcademicRank);
    }

    [Fact]
    public async Task Update_ByDepartmentHeadOrOwner_IsForbidden()
    {
        var lecturer = Seed(100, 10, "GV001");
        var request = new UpdateLecturerRequest { DepartmentId = 10, Code = "GV001", FullName = "X" };

        await Assert.ThrowsAsync<ForbiddenException>(() => Lecturers(Head(1, 10)).UpdateAsync(lecturer.Id, request, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Lecturers(Owner(100)).UpdateAsync(lecturer.Id, request, default));
    }

    [Fact]
    public async Task Update_CodeUsedByAnotherLecturer_Conflicts()
    {
        var first = Seed(100, 10, "GV001");
        Seed(101, 10, "GV002");
        var request = new UpdateLecturerRequest { DepartmentId = 10, Code = "GV002", FullName = "X" };

        await Assert.ThrowsAsync<ConflictException>(() => Lecturers(Office()).UpdateAsync(first.Id, request, default));
    }

    [Fact]
    public async Task UpdateMine_ChangesOnlyPersonalInformation()
    {
        Seed(100, 10, "GV001");

        var result = await Lecturers(Owner(100)).UpdateMineAsync(
            new UpdateMyLecturerRequest { FullName = "Tên tự sửa", Gender = "NU", Phone = "0901234567" }, default);

        Assert.Equal("Tên tự sửa", result.FullName);
        Assert.Equal("NU", result.Gender);
        Assert.Equal("GV001", result.Code);
        Assert.Equal(10, result.DepartmentId);
        Assert.Equal("PGS", result.AcademicRank);
    }

    [Fact]
    public async Task Mine_WithoutLinkedLecturer_IsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => Lecturers(Owner(101)).GetMineAsync(default));
        await Assert.ThrowsAsync<NotFoundException>(() => Profiles(Owner(101)).GetMineAsync(default));
    }

    [Fact]
    public async Task Delete_OnlyAdmin()
    {
        var lecturer = Seed(100, 10, "GV001");

        await Assert.ThrowsAsync<ForbiddenException>(() => Lecturers(Office()).DeleteAsync(lecturer.Id, default));
        await Lecturers(Admin()).DeleteAsync(lecturer.Id, default);
        await Assert.ThrowsAsync<NotFoundException>(() => Lecturers(Admin()).DeleteAsync(lecturer.Id, default));
    }

    [Fact]
    public async Task Delete_RemovesScientificProfileToo()
    {
        var lecturer = Seed(100, 10, "GV001");
        await Profiles(Owner(100)).UpsertAsync(lecturer.Id, new ScientificProfileRequest { Expertise = "AI" }, default);

        await Lecturers(Admin()).DeleteAsync(lecturer.Id, default);

        Assert.Empty(_profiles.Items);
    }

    // ---- Phạm vi đọc hồ sơ ----

    [Fact]
    public async Task Read_AllowedForOwnerOrScopeHolders()
    {
        var lecturer = Seed(100, 20, "GV001");

        foreach (var user in new[] { Owner(100), Head(1, 20), Dean(1), Office(), Board() })
            Assert.Equal(lecturer.Id, (await Lecturers(user).GetByIdAsync(lecturer.Id, default)).Id);
    }

    [Fact]
    public async Task Read_ForbiddenOutsideScope()
    {
        var lecturer = Seed(100, 20, "GV001");

        foreach (var user in new[] { Owner(101), Head(1, 10), Head(2, 30), Dean(2) })
            await Assert.ThrowsAsync<ForbiddenException>(() => Lecturers(user).GetByIdAsync(lecturer.Id, default));
    }

    [Fact]
    public async Task Read_UnknownId_IsNotFound() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Lecturers(Office()).GetByIdAsync(404, default));

    [Fact]
    public void Visibility_ReflectsAssignments()
    {
        var user = new FakeUser(7,
            new RoleAssignmentDto(Roles.Lecturer, Scopes.Personal, null, null),
            new RoleAssignmentDto(Roles.DepartmentHead, Scopes.Department, 1, 10),
            new RoleAssignmentDto(Roles.FacultyDean, Scopes.Faculty, 2, null));

        var visibility = LecturerAccess.VisibilityOf(user);

        Assert.False(visibility.All);
        Assert.Equal(new long[] { 10 }, visibility.DepartmentIds.ToArray());
        Assert.Equal(new long[] { 2 }, visibility.FacultyIds.ToArray());
        Assert.Equal(7, visibility.OwnAccountId);
        Assert.True(LecturerAccess.VisibilityOf(Board()).All);
    }

    // ---- Lý lịch khoa học ----

    [Fact]
    public async Task Profile_NotCreatedYet_ReturnsEmptyResponse()
    {
        var lecturer = Seed(100, 10, "GV001");

        var result = await Profiles(Owner(100)).GetAsync(lecturer.Id, default);

        Assert.Equal(lecturer.Id, result.LecturerId);
        Assert.Null(result.UpdatedAt);
        Assert.Null(result.Expertise);
    }

    [Fact]
    public async Task Profile_OwnerUpsertsTwice_KeepsSingleRow()
    {
        var lecturer = Seed(100, 10, "GV001");
        var service = Profiles(Owner(100));

        await service.UpsertAsync(lecturer.Id, new ScientificProfileRequest { Expertise = " Khoa học dữ liệu " }, default);
        var second = await service.UpsertMineAsync(new ScientificProfileRequest { ResearchFields = "NLP" }, default);

        Assert.Single(_profiles.Items);
        Assert.Null(second.Expertise);
        Assert.Equal("NLP", second.ResearchFields);
        Assert.NotNull(second.UpdatedAt);
    }

    [Fact]
    public async Task Profile_Edit_OwnerAndOfficeOnly()
    {
        var lecturer = Seed(100, 10, "GV001");
        var request = new ScientificProfileRequest { Expertise = "X" };

        await Profiles(Office()).UpsertAsync(lecturer.Id, request, default);
        foreach (var user in new[] { Head(1, 10), Dean(1), Board(), Owner(101) })
            await Assert.ThrowsAsync<ForbiddenException>(() => Profiles(user).UpsertAsync(lecturer.Id, request, default));
    }

    [Fact]
    public async Task Profile_Read_FollowsLecturerScope()
    {
        var lecturer = Seed(100, 10, "GV001");

        await Profiles(Head(1, 10)).GetAsync(lecturer.Id, default);
        await Profiles(Dean(1)).GetAsync(lecturer.Id, default);
        await Assert.ThrowsAsync<ForbiddenException>(() => Profiles(Head(1, 20)).GetAsync(lecturer.Id, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Profiles(Owner(101)).GetAsync(lecturer.Id, default));
    }

    // ---- Fakes ----

    [Fact]
    public async Task Profile_Details_RoundTripsAllTemplateSections()
    {
        var lecturer = Seed(100, 10, "GV001");
        var service = Profiles(Owner(100));
        var details = new ScientificProfileDetails
        {
            BirthPlace = "TP. Hồ Chí Minh", Hometown = "Đồng Nai", Ethnicity = "Kinh",
            HighestDegreeYear = 2020, HighestDegreeCountry = "Việt Nam", AcademicRankYear = 2022,
            WorkUnit = "Trường Đại học", ContactAddress = "12 Nguyễn Huệ", OfficePhone = "02812345678",
            HomePhone = "02511234567", Fax = "02812345679", IdentityNumber = "012345678901",
            IdentityIssuedDate = new(2021, 1, 1), IdentityIssuedPlace = "Cục CSQLHC",
            Education = [new() { Level = "TIEN_SI", Institution = "ĐHQG", Specialization = "CNTT",
                TrainingMode = "Chính quy", Country = "Việt Nam", GraduationYear = 2020, ThesisTitle = "Học máy" }],
            Languages = [new() { Name = "Tiếng Anh", Proficiency = "B2" }, new() { Name = "Tiếng Pháp", Proficiency = "Đọc hiểu" }],
            Employment = [new() { FromDate = new(2020, 1, 1), Organization = "Trường Đại học", Position = "Giảng viên" }],
            ResearchProjects = [new() { Title = "Nghiên cứu AI", StartYear = 2022, Level = "Bộ", Responsibility = "Chủ nhiệm" }],
            Publications = [new() { Title = "Mô hình học máy", Year = 2023, Journal = "Tạp chí CNTT" }],
            DeclarationPlace = "TP. Hồ Chí Minh", DeclarationDate = new(2024, 1, 1)
        };
        await service.UpsertMineAsync(new() { Expertise = "AI", Details = details }, default);
        // Đọc từ JSON đã lưu thay vì dùng lại cùng đối tượng request.
        details.BirthPlace = "Thay đổi sau lưu";
        var result = await service.GetMineAsync(default);
        Assert.Equal("TP. Hồ Chí Minh", result.Details.BirthPlace);
        Assert.Equal("012345678901", result.Details.IdentityNumber);
        Assert.Equal("Học máy", Assert.Single(result.Details.Education).ThesisTitle);
        Assert.Equal(2, result.Details.Languages.Count);
        Assert.Null(Assert.Single(result.Details.Employment).ToDate);
        Assert.Equal("Chủ nhiệm", Assert.Single(result.Details.ResearchProjects).Responsibility);
        Assert.Equal("Tạp chí CNTT", Assert.Single(result.Details.Publications).Journal);
        Assert.Equal(new DateOnly(2024, 1, 1), result.Details.DeclarationDate);
        Assert.Equal("AI", result.Expertise);
    }

    [Fact]
    public async Task Profile_LegacyClientPreservesDetails_ExplicitEmptyObjectClearsThem()
    {
        Seed(100, 10, "GV001");
        var service = Profiles(Owner(100));
        await service.UpsertMineAsync(new() { Details = new() { Hometown = "Hà Nội",
            Languages = [new() { Name = "Anh", Proficiency = "B2" }] } }, default);
        var legacy = await service.UpsertMineAsync(new() { Expertise = "NLP" }, default);
        Assert.Equal("Hà Nội", legacy.Details.Hometown);
        Assert.Single(legacy.Details.Languages);
        var cleared = await service.UpsertMineAsync(new() { Expertise = "NLP", Details = new() }, default);
        Assert.Null(cleared.Details.Hometown);
        Assert.Empty(cleared.Details.Languages);
        Assert.Equal("NLP", cleared.Expertise);
        Assert.Single(_profiles.Items);
    }

    [Fact]
    public async Task Profile_InvalidDetailsDoNotOverwriteSavedData()
    {
        Seed(100, 10, "GV001");
        var service = Profiles(Owner(100));
        await service.UpsertMineAsync(new() { Expertise = "AI", Details = new() { Hometown = "Huế" } }, default);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpsertMineAsync(new()
        {
            Expertise = "Sai", Details = new() { Employment = [new() { FromDate = new(2023, 1, 1),
                ToDate = new(2022, 1, 1), Organization = "Trường", Position = "GV" }] }
        }, default));
        var result = await service.GetMineAsync(default);
        Assert.Equal("AI", result.Expertise);
        Assert.Equal("Huế", result.Details.Hometown);
    }

    [Fact]
    public async Task Profile_RejectsNullRowsNullListsAndOversizedValues()
    {
        Seed(100, 10, "GV001");
        var service = Profiles(Owner(100));
        ScientificProfileDetails[] invalid = [
            new() { Education = null! }, new() { Languages = [null!] },
            new() { BirthPlace = new string('x', 201) },
            new() { Languages = Enumerable.Range(0, 101).Select(_ => new LanguageEntry { Name = "Anh", Proficiency = "B2" }).ToList() },
            new() { Education = [new() { Level = "KHAC", Institution = "Trường", Specialization = "CNTT" }] },
            new() { Publications = [new() { Title = "  ", Journal = "Tạp chí" }] }
        ];
        foreach (var details in invalid)
            await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpsertMineAsync(new() { Details = details }, default));
        Assert.Empty(_profiles.Items);
    }

    [Fact]
    public async Task Profile_FutureCompletionAllowed_ButFuturePublicationAndReversedYearsRejected()
    {
        Seed(100, 10, "GV001");
        var service = Profiles(Owner(100));
        var year = DateTime.UtcNow.Year;
        var saved = await service.UpsertMineAsync(new() { Details = new() { ResearchProjects = [new()
            { Title = "AI", StartYear = year, EndYear = year + 1, Level = "Trường", Responsibility = "Thành viên" }] } }, default);
        Assert.Equal(year + 1, Assert.Single(saved.Details.ResearchProjects).EndYear);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpsertMineAsync(new() { Details = new()
            { Publications = [new() { Title = "Bài báo", Year = year + 1, Journal = "Tạp chí" }] } }, default));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.UpsertMineAsync(new() { Details = new()
            { ResearchProjects = [new() { Title = "AI", StartYear = 2023, EndYear = 2022, Level = "Bộ", Responsibility = "Chủ nhiệm" }] } }, default));
    }

    [Fact]
    public async Task Profile_DetailedDataRetainsExistingScopeRules()
    {
        var lecturer = Seed(100, 10, "GV001");
        var request = new ScientificProfileRequest { Details = new() { Hometown = "Huế" } };
        await Profiles(Office()).UpsertAsync(lecturer.Id, request, default);
        Assert.Equal("Huế", (await Profiles(Head(1, 10)).GetAsync(lecturer.Id, default)).Details.Hometown);
        await Assert.ThrowsAsync<ForbiddenException>(() => Profiles(Head(1, 10)).UpsertAsync(lecturer.Id, request, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Profiles(Owner(101)).GetAsync(lecturer.Id, default));
    }

    private sealed class FakeUser(long? id, params RoleAssignmentDto[] assignments) : ICurrentUser
    {
        public bool IsAuthenticated => Id.HasValue;
        public long? Id { get; } = id;
        public IReadOnlyList<RoleAssignmentDto> Assignments { get; } = assignments;
        public bool IsInRole(string role) => Assignments.Any(a => a.Role == role);
        public bool CanAccess(long? facultyId, long? departmentId) =>
            ScopeRules.CanAccess(Assignments, facultyId, departmentId);
    }

    private sealed class FakeAccounts(params long[] knownIds) : IAccountRepository
    {
        public Task<IReadOnlyList<LecturerAccountOptionResponse>> GetAvailableLecturerAccountsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<LecturerAccountOptionResponse>>(knownIds
                .Select(id => new LecturerAccountOptionResponse(id, "user" + id, "Tài khoản " + id, null, [Roles.Lecturer]))
                .ToList());
        public Task<Account?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult<Account?>(knownIds.Contains(id) ? new Account("user" + id, "", "Tài khoản " + id) : null);
        public Task<Account?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task<bool> ExistsByUsernameAsync(string username, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
        public Task AddAsync(Account account, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static void AssignId(BaseEntity entity, long id) =>
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(entity, id);

    private sealed class FakeLecturerRepository : ILecturerRepository
    {
        private static readonly Dictionary<long, long> FacultyOf = new() { [10] = 1, [20] = 1, [30] = 2 };
        private readonly List<Lecturer> _items = [];
        private long _nextId = 1;

        public void Add(Lecturer lecturer)
        {
            AssignId(lecturer, _nextId++);
            _items.Add(lecturer);
        }

        private static LecturerResponse View(Lecturer l) => new(
            l.Id, l.AccountId, l.Code, l.FullName, l.BirthDate, l.Gender, l.Email, l.Phone,
            l.AcademicRank, l.Degree, l.Position, l.DepartmentId, "Bộ môn " + l.DepartmentId,
            FacultyOf[l.DepartmentId], "Khoa " + FacultyOf[l.DepartmentId]);

        public Task<LecturerResponse?> GetViewAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Where(x => x.Id == id).Select(View).Cast<LecturerResponse?>().FirstOrDefault());

        public Task<LecturerResponse?> GetViewByAccountIdAsync(long accountId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Where(x => x.AccountId == accountId).Select(View).Cast<LecturerResponse?>().FirstOrDefault());

        public Task<PagedResult<LecturerResponse>> SearchAsync(
            LecturerSearchQuery query, LecturerVisibility visibility, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Truy vấn tìm kiếm được kiểm tra bằng smoke_lecturers.py trên database thật.");

        public Task<Lecturer?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<Lecturer?> GetByAccountIdAsync(long accountId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(x => x.AccountId == accountId));

        public Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken) =>
            Task.FromResult(FacultyOf.ContainsKey(departmentId));

        public Task<bool> ExistsByCodeAsync(string code, long? excludeId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Any(x => x.Code == code && x.Id != excludeId));

        public Task<bool> ExistsByAccountIdAsync(long accountId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.Any(x => x.AccountId == accountId));

        public Task AddAsync(Lecturer lecturer, CancellationToken cancellationToken)
        {
            Add(lecturer);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(Lecturer lecturer, CancellationToken cancellationToken)
        {
            _items.Remove(lecturer);
            OnRemoved?.Invoke(lecturer.Id);
            return Task.CompletedTask;
        }

        public Action<long>? OnRemoved { get; set; }
        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeProfileRepository : IScientificProfileRepository
    {
        public List<ScientificProfile> Items { get; } = [];

        public Task<ScientificProfile?> GetByLecturerIdAsync(long lecturerId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.LecturerId == lecturerId));

        public Task AddAsync(ScientificProfile profile, CancellationToken cancellationToken)
        {
            Items.Add(profile);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public LecturerServiceTests()
    {
        // Mô phỏng khóa ngoại: xóa giảng viên thì lý lịch của giảng viên đó cũng bị xóa.
        _lecturers.OnRemoved = id => _profiles.Items.RemoveAll(p => p.LecturerId == id);
    }
}
