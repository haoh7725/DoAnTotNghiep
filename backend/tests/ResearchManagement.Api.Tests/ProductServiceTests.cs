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

public class ProductServiceTests
{
    // Dữ liệu nền: bộ môn 10, 20 thuộc khoa 1; bộ môn 30 thuộc khoa 2.
    // Giảng viên 1,2,5: bộ môn 10 (tài khoản 101,102,105); 3: bộ môn 20 (103); 4: bộ môn 30 (104).
    private readonly FakeLecturers _lecturers = new();
    private readonly FakeProductTypes _types = new();
    private readonly FakeProducts _products;

    public ProductServiceTests() => _products = new FakeProducts(_lecturers, _types);

    private static FakeUser Lecturer(long accountId) =>
        new(accountId, new RoleAssignmentDto(Roles.Lecturer, Scopes.Personal, null, null));
    private static FakeUser Office() => new(900, new RoleAssignmentDto(Roles.ResearchOffice, Scopes.Global, null, null));
    private static FakeUser Admin() => new(901, new RoleAssignmentDto(Roles.Admin, Scopes.Global, null, null));
    private static FakeUser Head(long faculty, long department) =>
        new(902, new RoleAssignmentDto(Roles.DepartmentHead, Scopes.Department, faculty, department));
    private static FakeUser Dean(long faculty) =>
        new(903, new RoleAssignmentDto(Roles.FacultyDean, Scopes.Faculty, faculty, null));

    private ProductService Products(ICurrentUser user) => new(_products, _types, _lecturers, user, TimeProvider.System);

    private ProductAuthorService Authors(ICurrentUser user) => new(_products, _types, _lecturers, user);

    private static CreateProductRequest NewArticle(string title = "Học máy cho dự báo điểm") => new()
    {
        ProductTypeId = FakeProductTypes.ArticleId, AcademicYearId = 1, Title = title,
    };

    private async Task<ProductDetailResponse> CreateAs(ICurrentUser user, CreateProductRequest? request = null) =>
        await Products(user).CreateAsync(request ?? NewArticle(), default);

    private static void SetReviewStatus(Product product, string status) =>
        typeof(Product).GetProperty(nameof(Product.ReviewStatus))!.SetValue(product, status);

    private static void Submit(FakeProducts repo, long productId) =>
        SetReviewStatus(repo.Items.Single(p => p.Id == productId), ReviewStatuses.Pending);

    // ---- Thêm sản phẩm ----

    [Fact]
    public async Task Create_ByLecturer_MakesThemMainAuthorAndStartsAsDraftWriting()
    {
        var result = await CreateAs(Lecturer(101), NewArticle("  Học máy cho dự báo điểm  "));

        Assert.Equal("Học máy cho dự báo điểm", result.Title);
        Assert.Equal("BAI_BAO", result.ProductTypeCode);
        Assert.Equal(ReviewStatuses.Draft, result.ReviewStatus);
        Assert.Equal(ArticleStatuses.Writing, result.ArticleStatus);
        Assert.Equal(1, result.Version);
        Assert.StartsWith($"BB-{DateTime.UtcNow.Year}-", result.Code);
        Assert.Equal(101, result.CreatedByAccountId);

        var author = Assert.Single(result.Authors);
        Assert.Equal(1, author.LecturerId);
        Assert.Equal(AuthorRoles.Main, author.Role);
        Assert.Equal(1, author.Order);

        Assert.True(result.Permissions!.CanEdit);
        Assert.True(result.Permissions.CanManageAuthors);
        Assert.Equal([ArticleStatuses.UnderReview], result.Permissions.NextArticleStatuses);
    }

    [Fact]
    public async Task Create_WithCoAuthors_NumbersThemAfterTheMainAuthor()
    {
        var request = NewArticle();
        request.CoAuthors =
        [
            new() { LecturerId = 3, Role = AuthorRoles.CoAuthor },
            new() { LecturerId = 2, Role = AuthorRoles.Corresponding },
        ];

        var result = await CreateAs(Lecturer(101), request);

        Assert.Equal([1L, 3L, 2L], result.Authors.Select(a => a.LecturerId));
        Assert.Equal([1, 2, 3], result.Authors.Select(a => a.Order));
        Assert.Equal([AuthorRoles.Main, AuthorRoles.CoAuthor, AuthorRoles.Corresponding], result.Authors.Select(a => a.Role));
    }

    [Fact]
    public async Task Create_CoAuthorWithMainRoleOrDuplicateOfMain_IsRejected()
    {
        var mainRole = NewArticle();
        mainRole.CoAuthors = [new() { LecturerId = 2, Role = AuthorRoles.Main }];
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAs(Lecturer(101), mainRole));

        var self = NewArticle();
        self.CoAuthors = [new() { LecturerId = 1, Role = AuthorRoles.CoAuthor }];
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAs(Lecturer(101), self));

        var unknown = NewArticle();
        unknown.CoAuthors = [new() { LecturerId = 99, Role = AuthorRoles.CoAuthor }];
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAs(Lecturer(101), unknown));
    }

    [Fact]
    public async Task Create_NonArticle_HasNoArticleStatusAndRejectsOne()
    {
        var project = NewArticle();
        project.ProductTypeId = FakeProductTypes.ProjectId;
        var result = await CreateAs(Lecturer(101), project);

        Assert.Null(result.ArticleStatus);
        Assert.StartsWith($"DT-{DateTime.UtcNow.Year}-", result.Code);
        Assert.Empty(result.Permissions!.NextArticleStatuses);

        project.ArticleStatus = ArticleStatuses.Writing;
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAs(Lecturer(101), project));
    }

    [Fact]
    public async Task Create_AccountWithoutLecturerProfile_IsRejected() =>
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAs(Lecturer(555)));

    [Fact]
    public async Task Create_OnBehalfOfAnotherLecturer_OnlyForOfficeAndAdmin()
    {
        var request = NewArticle();
        request.MainAuthorLecturerId = 3;

        await Assert.ThrowsAsync<ForbiddenException>(() => CreateAs(Lecturer(101), request));

        var byOffice = await CreateAs(Office(), request);
        Assert.Equal(3, byOffice.Authors.Single().LecturerId);
        Assert.Equal(900, byOffice.CreatedByAccountId);

        // Người nhập hộ không có hồ sơ giảng viên mà không chọn tác giả chính thì bị từ chối.
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAs(Admin()));
    }

    [Fact]
    public async Task Create_ValidatesTypeYearDatesAndPublishedRequirements()
    {
        var badType = NewArticle(); badType.ProductTypeId = 99;
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAs(Lecturer(101), badType));

        var badYear = NewArticle(); badYear.AcademicYearId = 99;
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAs(Lecturer(101), badYear));

        var badDates = NewArticle();
        badDates.StartDate = new DateOnly(2026, 5, 1); badDates.EndDate = new DateOnly(2026, 4, 1);
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAs(Lecturer(101), badDates));

        var publishedWithoutJournal = NewArticle();
        publishedWithoutJournal.ArticleStatus = ArticleStatuses.Published;
        await Assert.ThrowsAsync<BusinessRuleException>(() => CreateAs(Lecturer(101), publishedWithoutJournal));

        publishedWithoutJournal.JournalName = "Journal of Tests";
        publishedWithoutJournal.PublishedDate = new DateOnly(2026, 3, 9);
        var ok = await CreateAs(Lecturer(101), publishedWithoutJournal);
        Assert.Equal(2026, ok.PublicationYear); // lấy từ ngày xuất bản
    }

    [Fact]
    public async Task Create_DuplicateDoi_ConflictsCaseInsensitively()
    {
        var first = NewArticle(); first.Doi = " 10.1000/ABC ";
        await CreateAs(Lecturer(101), first);

        var second = NewArticle("Bài khác"); second.Doi = "10.1000/abc";
        await Assert.ThrowsAsync<ConflictException>(() => CreateAs(Lecturer(102), second));
    }

    // ---- Xem chi tiết và phạm vi ----

    [Fact]
    public async Task Draft_IsVisibleOnlyToParticipantsAndAdmin()
    {
        var request = NewArticle();
        request.CoAuthors = [new() { LecturerId = 2, Role = AuthorRoles.CoAuthor }];
        var created = await CreateAs(Lecturer(101), request);

        Assert.Equal(created.Id, (await Products(Lecturer(102)).GetByIdAsync(created.Id, default)).Id); // đồng tác giả
        Assert.Equal(created.Id, (await Products(Admin()).GetByIdAsync(created.Id, default)).Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => Products(Lecturer(105)).GetByIdAsync(created.Id, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Products(Head(1, 10)).GetByIdAsync(created.Id, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Products(Office()).GetByIdAsync(created.Id, default));
        await Assert.ThrowsAsync<NotFoundException>(() => Products(Lecturer(101)).GetByIdAsync(404, default));
    }

    [Fact]
    public async Task Submitted_IsVisibleWithinScopeOfAnAuthorsUnit()
    {
        var created = await CreateAs(Lecturer(101)); // tác giả chính thuộc bộ môn 10, khoa 1
        Submit(_products, created.Id);

        Assert.Equal(created.Id, (await Products(Head(1, 10)).GetByIdAsync(created.Id, default)).Id);
        Assert.Equal(created.Id, (await Products(Dean(1)).GetByIdAsync(created.Id, default)).Id);
        Assert.Equal(created.Id, (await Products(Office()).GetByIdAsync(created.Id, default)).Id);

        await Assert.ThrowsAsync<ForbiddenException>(() => Products(Head(1, 20)).GetByIdAsync(created.Id, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Products(Dean(2)).GetByIdAsync(created.Id, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Products(Lecturer(105)).GetByIdAsync(created.Id, default));
    }

    [Fact]
    public async Task Permissions_OnlyCreatorOrMainAuthorCanEdit_AndOnlyBeforeSubmission()
    {
        var request = NewArticle();
        request.CoAuthors = [new() { LecturerId = 2, Role = AuthorRoles.CoAuthor }];
        var created = await CreateAs(Lecturer(101), request);

        Assert.False((await Products(Lecturer(102)).GetByIdAsync(created.Id, default)).Permissions!.CanEdit);
        Assert.True((await Products(Admin()).GetByIdAsync(created.Id, default)).Permissions!.CanEdit);

        Submit(_products, created.Id);
        var after = await Products(Lecturer(101)).GetByIdAsync(created.Id, default);
        Assert.False(after.Permissions!.CanEdit);
        Assert.False(after.Permissions.CanChangeArticleStatus);
        Assert.Empty(after.Permissions.NextArticleStatuses);
    }

    // ---- Sửa sản phẩm ----

    [Fact]
    public async Task Update_ByMainAuthor_SavesTrimmedContentAndKeepsTypeAndStatus()
    {
        var created = await CreateAs(Lecturer(101));

        var result = await Products(Lecturer(101)).UpdateAsync(created.Id, new UpdateProductRequest
        {
            AcademicYearId = 2, Title = "  Tên mới  ", JournalName = " Tạp chí X ", JournalIndex = "   ",
            PublicationYear = 2025, Doi = "10.1/x", WorkScore = 1.5m,
        }, default);

        Assert.Equal("Tên mới", result.Title);
        Assert.Equal("Tạp chí X", result.JournalName);
        Assert.Null(result.JournalIndex);
        Assert.Equal(2, result.AcademicYearId);
        Assert.Equal(2025, result.PublicationYear);
        Assert.Equal(1.5m, result.WorkScore);
        Assert.Equal(created.Code, result.Code);
        Assert.Equal("BAI_BAO", result.ProductTypeCode);
        Assert.Equal(ArticleStatuses.Writing, result.ArticleStatus);
    }

    [Fact]
    public async Task Update_ByCoAuthorOrOutsider_IsForbidden()
    {
        var request = NewArticle();
        request.CoAuthors = [new() { LecturerId = 2, Role = AuthorRoles.CoAuthor }];
        var created = await CreateAs(Lecturer(101), request);
        var update = new UpdateProductRequest { AcademicYearId = 1, Title = "Đổi tên" };

        await Assert.ThrowsAsync<ForbiddenException>(() => Products(Lecturer(102)).UpdateAsync(created.Id, update, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Products(Lecturer(105)).UpdateAsync(created.Id, update, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Products(Office()).UpdateAsync(created.Id, update, default));
        await Assert.ThrowsAsync<NotFoundException>(() => Products(Lecturer(101)).UpdateAsync(404, update, default));
    }

    [Fact]
    public async Task Update_AfterSubmission_IsRejectedButAllowedWhenRevisionRequested()
    {
        var created = await CreateAs(Lecturer(101));
        var update = new UpdateProductRequest { AcademicYearId = 1, Title = "Đổi tên" };

        foreach (var locked in new[] { ReviewStatuses.Pending, ReviewStatuses.Approved, ReviewStatuses.Rejected })
        {
            SetReviewStatus(_products.Items.Single(), locked);
            await Assert.ThrowsAsync<BusinessRuleException>(() => Products(Lecturer(101)).UpdateAsync(created.Id, update, default));
        }

        SetReviewStatus(_products.Items.Single(), ReviewStatuses.NeedsRevision);
        Assert.Equal("Đổi tên", (await Products(Lecturer(101)).UpdateAsync(created.Id, update, default)).Title);
    }

    [Fact]
    public async Task Update_DoiOfAnotherProduct_ConflictsButOwnDoiIsFine()
    {
        var first = NewArticle(); first.Doi = "10.1/a";
        var firstCreated = await CreateAs(Lecturer(101), first);
        var second = NewArticle("Bài hai"); second.Doi = "10.1/b";
        var secondCreated = await CreateAs(Lecturer(102), second);

        await Assert.ThrowsAsync<ConflictException>(() => Products(Lecturer(102)).UpdateAsync(
            secondCreated.Id, new UpdateProductRequest { AcademicYearId = 1, Title = "Bài hai", Doi = "10.1/A" }, default));

        var same = await Products(Lecturer(101)).UpdateAsync(
            firstCreated.Id, new UpdateProductRequest { AcademicYearId = 1, Title = "Bài một", Doi = "10.1/a" }, default);
        Assert.Equal("10.1/a", same.Doi);
    }

    // ---- Trạng thái bài báo ----

    [Fact]
    public async Task ArticleStatus_FollowsAllowedTransitions()
    {
        var created = await CreateAs(Lecturer(101));
        var owner = Products(Lecturer(101));

        var underReview = await owner.ChangeArticleStatusAsync(
            created.Id, new ChangeArticleStatusRequest { Status = ArticleStatuses.UnderReview }, default);
        Assert.Equal(ArticleStatuses.UnderReview, underReview.ArticleStatus);
        Assert.Equal([ArticleStatuses.Reviewed, ArticleStatuses.Writing], underReview.Permissions!.NextArticleStatuses);

        // Không được nhảy cóc sang Đã xuất bản, cũng không đổi sang chính trạng thái hiện tại.
        await Assert.ThrowsAsync<BusinessRuleException>(() => owner.ChangeArticleStatusAsync(
            created.Id, new ChangeArticleStatusRequest { Status = ArticleStatuses.Published }, default));
        await Assert.ThrowsAsync<BusinessRuleException>(() => owner.ChangeArticleStatusAsync(
            created.Id, new ChangeArticleStatusRequest { Status = ArticleStatuses.UnderReview }, default));

        var reviewed = await owner.ChangeArticleStatusAsync(
            created.Id, new ChangeArticleStatusRequest { Status = ArticleStatuses.Reviewed }, default);
        Assert.Contains(ArticleStatuses.Published, reviewed.Permissions!.NextArticleStatuses);
    }

    [Fact]
    public async Task ArticleStatus_PublishedRequiresJournalAndYear_ThenIsFinal()
    {
        var created = await CreateAs(Lecturer(101));
        var owner = Products(Lecturer(101));
        await owner.ChangeArticleStatusAsync(created.Id, new() { Status = ArticleStatuses.UnderReview }, default);
        await owner.ChangeArticleStatusAsync(created.Id, new() { Status = ArticleStatuses.Reviewed }, default);

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => owner.ChangeArticleStatusAsync(
            created.Id, new ChangeArticleStatusRequest { Status = ArticleStatuses.Published }, default));
        Assert.Contains("tên tạp chí", ex.Message);

        await owner.UpdateAsync(created.Id, new UpdateProductRequest
        { AcademicYearId = 1, Title = "Học máy", JournalName = "Tạp chí X", PublicationYear = 2026 }, default);
        var published = await owner.ChangeArticleStatusAsync(
            created.Id, new ChangeArticleStatusRequest { Status = ArticleStatuses.Published }, default);

        Assert.Equal(ArticleStatuses.Published, published.ArticleStatus);
        Assert.Empty(published.Permissions!.NextArticleStatuses);
        await Assert.ThrowsAsync<BusinessRuleException>(() => owner.ChangeArticleStatusAsync(
            created.Id, new ChangeArticleStatusRequest { Status = ArticleStatuses.Writing }, default));
    }

    [Fact]
    public async Task ArticleStatus_OnlyForArticlesAndOnlyByManagersBeforeSubmission()
    {
        var project = NewArticle(); project.ProductTypeId = FakeProductTypes.ProjectId;
        var projectCreated = await CreateAs(Lecturer(101), project);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Products(Lecturer(101)).ChangeArticleStatusAsync(
            projectCreated.Id, new ChangeArticleStatusRequest { Status = ArticleStatuses.UnderReview }, default));

        var request = NewArticle();
        request.CoAuthors = [new() { LecturerId = 2, Role = AuthorRoles.CoAuthor }];
        var created = await CreateAs(Lecturer(101), request);
        var change = new ChangeArticleStatusRequest { Status = ArticleStatuses.UnderReview };

        await Assert.ThrowsAsync<ForbiddenException>(() => Products(Lecturer(102)).ChangeArticleStatusAsync(created.Id, change, default));

        Submit(_products, created.Id);
        await Assert.ThrowsAsync<BusinessRuleException>(() => Products(Lecturer(101)).ChangeArticleStatusAsync(created.Id, change, default));
    }

    [Fact]
    public void ArticleStatuses_TransitionTable()
    {
        Assert.Equal(ArticleStatuses.All, ArticleStatuses.NextFrom(null)); // chưa có trạng thái: chọn tự do
        Assert.True(ArticleStatuses.CanTransition(ArticleStatuses.Reviewed, ArticleStatuses.UnderReview)); // nộp lại sau nhận xét
        Assert.False(ArticleStatuses.CanTransition(ArticleStatuses.Writing, ArticleStatuses.Reviewed));
        Assert.False(ArticleStatuses.CanTransition(ArticleStatuses.Writing, ArticleStatuses.Writing));
        Assert.Empty(ArticleStatuses.NextFrom(ArticleStatuses.Published));
        Assert.Empty(ArticleStatuses.NextFrom("KHONG_HOP_LE"));
    }

    // ---- Đồng tác giả ----

    private async Task<ProductDetailResponse> CreateWithCoAuthors(params long[] coAuthorIds)
    {
        var request = NewArticle();
        request.CoAuthors = coAuthorIds.Select(id => new ProductAuthorInput { LecturerId = id, Role = AuthorRoles.CoAuthor }).ToList();
        return await CreateAs(Lecturer(101), request);
    }

    [Fact]
    public async Task AddAuthor_AppendsAtTheEndAndRecordsCurrentDepartment()
    {
        var created = await CreateWithCoAuthors(2);

        var result = await Authors(Lecturer(101)).AddAsync(
            created.Id, new AddProductAuthorRequest { LecturerId = 4, Role = AuthorRoles.Corresponding }, default);

        Assert.Equal([1L, 2L, 4L], result.Select(a => a.LecturerId));
        Assert.Equal([1, 2, 3], result.Select(a => a.Order));
        Assert.Equal(30, result.Last().DepartmentId);
        Assert.Equal(2, result.Last().FacultyId);
    }

    [Fact]
    public async Task AddAuthor_RejectsDuplicatesSecondMainUnknownLecturerAndNonManagers()
    {
        var created = await CreateWithCoAuthors(2);
        var owner = Authors(Lecturer(101));

        await Assert.ThrowsAsync<ConflictException>(() => owner.AddAsync(
            created.Id, new AddProductAuthorRequest { LecturerId = 2 }, default));
        await Assert.ThrowsAsync<BusinessRuleException>(() => owner.AddAsync(
            created.Id, new AddProductAuthorRequest { LecturerId = 3, Role = AuthorRoles.Main }, default));
        await Assert.ThrowsAsync<BusinessRuleException>(() => owner.AddAsync(
            created.Id, new AddProductAuthorRequest { LecturerId = 99 }, default));
        await Assert.ThrowsAsync<ForbiddenException>(() => Authors(Lecturer(102)).AddAsync(
            created.Id, new AddProductAuthorRequest { LecturerId = 3 }, default));

        Submit(_products, created.Id);
        await Assert.ThrowsAsync<BusinessRuleException>(() => owner.AddAsync(
            created.Id, new AddProductAuthorRequest { LecturerId = 3 }, default));
    }

    [Fact]
    public async Task RemoveAuthor_CompactsOrder_AndProtectsTheMainAuthor()
    {
        var created = await CreateWithCoAuthors(2, 3, 4);
        var owner = Authors(Lecturer(101));

        var remaining = await owner.RemoveAsync(created.Id, 3, default);

        Assert.Equal([1L, 2L, 4L], remaining.Select(a => a.LecturerId));
        Assert.Equal([1, 2, 3], remaining.Select(a => a.Order));
        await Assert.ThrowsAsync<BusinessRuleException>(() => owner.RemoveAsync(created.Id, 1, default));
        await Assert.ThrowsAsync<NotFoundException>(() => owner.RemoveAsync(created.Id, 3, default));
    }

    [Fact]
    public async Task ReplaceAuthors_ReordersAndTransfersTheMainAuthorRole()
    {
        var created = await CreateWithCoAuthors(2, 3);

        var result = await Authors(Lecturer(101)).ReplaceAsync(created.Id, new ReplaceProductAuthorsRequest
        {
            Authors =
            [
                new() { LecturerId = 3, Role = AuthorRoles.Main },
                new() { LecturerId = 1, Role = AuthorRoles.Corresponding },
                new() { LecturerId = 5, Role = AuthorRoles.CoAuthor },
            ],
        }, default);

        Assert.Equal([3L, 1L, 5L], result.Select(a => a.LecturerId));
        Assert.Equal([1, 2, 3], result.Select(a => a.Order));
        Assert.Equal(AuthorRoles.Main, result[0].Role);
        Assert.Equal(AuthorRoles.Corresponding, result[1].Role);

        // Người tạo vẫn được quản lý dù không còn là tác giả chính; giảng viên 3 giờ cũng quản lý được.
        Assert.True((await Products(Lecturer(101)).GetByIdAsync(created.Id, default)).Permissions!.CanEdit);
        Assert.True((await Products(Lecturer(103)).GetByIdAsync(created.Id, default)).Permissions!.CanEdit);
    }

    [Fact]
    public async Task ReplaceAuthors_ValidatesTheWholeList()
    {
        var created = await CreateWithCoAuthors(2);
        var owner = Authors(Lecturer(101));
        Task Replace(params ProductAuthorInput[] authors) =>
            owner.ReplaceAsync(created.Id, new ReplaceProductAuthorsRequest { Authors = authors.ToList() }, default);

        await Assert.ThrowsAsync<BusinessRuleException>(() => Replace()); // rỗng
        await Assert.ThrowsAsync<BusinessRuleException>(() => Replace( // không có tác giả chính
            new ProductAuthorInput { LecturerId = 1, Role = AuthorRoles.CoAuthor }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Replace( // hai tác giả chính
            new ProductAuthorInput { LecturerId = 1, Role = AuthorRoles.Main }, new ProductAuthorInput { LecturerId = 2, Role = AuthorRoles.Main }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Replace( // trùng giảng viên
            new ProductAuthorInput { LecturerId = 1, Role = AuthorRoles.Main }, new ProductAuthorInput { LecturerId = 1, Role = AuthorRoles.CoAuthor }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Replace( // vai trò lạ
            new ProductAuthorInput { LecturerId = 1, Role = AuthorRoles.Main }, new ProductAuthorInput { LecturerId = 2, Role = "KHAC" }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Replace( // giảng viên không tồn tại
            new ProductAuthorInput { LecturerId = 1, Role = AuthorRoles.Main }, new ProductAuthorInput { LecturerId = 99, Role = AuthorRoles.CoAuthor }));

        var tooMany = Enumerable.Range(0, 31)
            .Select(i => new ProductAuthorInput { LecturerId = 1 + i, Role = i == 0 ? AuthorRoles.Main : AuthorRoles.CoAuthor })
            .ToArray();
        await Assert.ThrowsAsync<BusinessRuleException>(() => Replace(tooMany));

        // Danh sách vẫn nguyên vẹn sau các lần bị từ chối.
        Assert.Equal([1L, 2L], (await owner.GetAsync(created.Id, default)).Select(a => a.LecturerId));
    }

    [Fact]
    public async Task GetAuthors_FollowsReadAccess()
    {
        var created = await CreateWithCoAuthors(2);

        Assert.Equal(2, (await Authors(Lecturer(102)).GetAsync(created.Id, default)).Count);
        await Assert.ThrowsAsync<ForbiddenException>(() => Authors(Lecturer(105)).GetAsync(created.Id, default));
        await Assert.ThrowsAsync<NotFoundException>(() => Authors(Lecturer(101)).GetAsync(404, default));
    }

    // ---- Phạm vi liệt kê ----

    [Fact]
    public void Visibility_ReflectsAssignments()
    {
        var user = new FakeUser(7,
            new RoleAssignmentDto(Roles.FacultyDean, Scopes.Faculty, 1, null),
            new RoleAssignmentDto(Roles.DepartmentHead, Scopes.Department, 2, 30));

        var visibility = ProductAccess.VisibilityOf(user);

        Assert.False(visibility.IsAdmin);
        Assert.False(visibility.All);
        Assert.Equal([1L], visibility.FacultyIds);
        Assert.Equal([30L], visibility.DepartmentIds);
        Assert.Equal(7, visibility.AccountId);

        Assert.True(ProductAccess.VisibilityOf(Admin()).IsAdmin);
        Assert.True(ProductAccess.VisibilityOf(Office()).All);
    }

    // ---- Fakes ----

    private sealed class FakeUser(long? id, params RoleAssignmentDto[] assignments) : ICurrentUser
    {
        public bool IsAuthenticated => Id.HasValue;
        public long? Id { get; } = id;
        public IReadOnlyList<RoleAssignmentDto> Assignments { get; } = assignments;
        public bool IsInRole(string role) => Assignments.Any(a => a.Role == role);
        public bool CanAccess(long? facultyId, long? departmentId) =>
            ScopeRules.CanAccess(Assignments, facultyId, departmentId);
    }

    private static void AssignId(BaseEntity entity, long id) =>
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))!.SetValue(entity, id);

    private sealed class FakeLecturers : ILecturerRepository
    {
        private static readonly Dictionary<long, long> FacultyOf = new() { [10] = 1, [20] = 1, [30] = 2 };
        private readonly List<LecturerResponse> _items =
        [
            Make(1, 101, 10), Make(2, 102, 10), Make(3, 103, 20), Make(4, 104, 30), Make(5, 105, 10),
        ];

        private static LecturerResponse Make(long id, long accountId, long departmentId) => new(
            id, accountId, "GV" + id, "Giảng viên " + id, null, null, null, null, null, null, null,
            departmentId, "Bộ môn " + departmentId, FacultyOf[departmentId], "Khoa " + FacultyOf[departmentId]);

        // Danh sách tác giả tối đa 30 người nên cần thêm giảng viên cho bài kiểm tra "quá nhiều tác giả".
        public FakeLecturers() => _items.AddRange(Enumerable.Range(6, 30).Select(i => Make(i, 100 + i, 10)));

        public Task<LecturerResponse?> GetViewAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(x => x.Id == id));

        public Task<LecturerResponse?> GetViewByAccountIdAsync(long accountId, CancellationToken cancellationToken) =>
            Task.FromResult(_items.FirstOrDefault(x => x.AccountId == accountId));

        public Task<PagedResult<LecturerResponse>> SearchAsync(
            LecturerSearchQuery query, LecturerVisibility visibility, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
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
        public const long ProjectId = 2;
        private readonly List<ProductType> _items = [];

        public FakeProductTypes()
        {
            var article = new ProductType(ProductTypeCodes.Article, "Bài báo"); AssignId(article, ArticleId);
            var project = new ProductType(ProductTypeCodes.Project, "Đề tài"); AssignId(project, ProjectId);
            _items.AddRange([article, project]);
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
        private long _nextId = 1;

        public List<Product> Items { get; } = [];

        public async Task<ProductDetailResponse?> GetViewAsync(long id, CancellationToken cancellationToken)
        {
            var p = Items.FirstOrDefault(x => x.Id == id);
            if (p is null) return null;
            var type = (await types.GetByIdAsync(p.ProductTypeId, cancellationToken))!;
            var creator = await lecturers.GetViewByAccountIdAsync(p.CreatedByAccountId, cancellationToken);
            return new ProductDetailResponse(
                p.Id, p.Code, p.Title, p.ProductTypeId, type.Code, type.Name, p.AcademicYearId, "NH" + p.AcademicYearId,
                p.CreatedByAccountId, creator?.FullName ?? "Người dùng " + p.CreatedByAccountId, p.PublicationYear,
                p.PublicationInfo, p.Doi, p.Isbn, p.Issn, p.JournalName, p.JournalIndex, p.JournalCategory, p.WorkScore,
                p.ResearchField, p.Publisher, p.ProjectLevel, p.HostUnit, p.ProjectObjective, p.ProjectContent,
                p.ExpectedResult, p.CertificateNumber, p.IssuingAuthority, p.StartDate, p.EndDate, p.SubmittedDate,
                p.PublishedDate, p.ArticleStatus, p.ReviewStatus, p.Version, await GetAuthorsAsync(id, cancellationToken), null);
        }

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

        public Task<PagedResult<ProductSummaryResponse>> SearchAsync(
            ProductSearchQuery query, ProductVisibility visibility, CancellationToken cancellationToken) =>
            throw new NotSupportedException("Truy vấn tìm kiếm cần kiểm tra trên PostgreSQL thật.");

        public Task<Product?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(x => x.Id == id));

        public Task<bool> AcademicYearExistsAsync(long academicYearId, CancellationToken cancellationToken) =>
            Task.FromResult(academicYearId is 1 or 2);

        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(x => x.Code == code));

        public Task<bool> DoiExistsAsync(string doi, long? excludeProductId, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(x => x.Doi != null
                && string.Equals(x.Doi, doi, StringComparison.OrdinalIgnoreCase) && x.Id != excludeProductId));

        public Task CreateAsync(Product product, IReadOnlyList<AuthorSlot> authors, CancellationToken cancellationToken)
        {
            AssignId(product, _nextId++);
            Items.Add(product);
            _authors[product.Id] = authors.ToList();
            return Task.CompletedTask;
        }

        public Task ReplaceAuthorsAsync(long productId, IReadOnlyList<AuthorSlot> authors, CancellationToken cancellationToken)
        {
            // Giống repository thật: người cũ giữ bộ môn đã ghi nhận, người mới nhận bộ môn hiện tại.
            var previous = _authors[productId].ToDictionary(s => s.LecturerId);
            _authors[productId] = authors
                .Select(s => previous.TryGetValue(s.LecturerId, out var old) ? s with { DepartmentId = old.DepartmentId } : s)
                .ToList();
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
