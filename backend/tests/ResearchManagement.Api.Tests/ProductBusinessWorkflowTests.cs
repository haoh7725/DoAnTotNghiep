using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ResearchManagement.Api.Controllers;
using ResearchManagement.Api.Contracts;
using ResearchManagement.Api.Infrastructure;
using ResearchManagement.Application.Auth;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Auth.Models;
using ResearchManagement.Application.Common;
using ResearchManagement.Application.Evaluations;
using ResearchManagement.Application.Products;
using ResearchManagement.Application.Products.Models;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Tests;

public sealed class ProductBusinessWorkflowTests
{
    [Theory]
    [InlineData("GIO", 240, 0, 200, 100, 120)]
    [InlineData("DIEM", 0, 80, 200, 100, 80)]
    [InlineData("GIO", 50, 999, 100, 100, 50)]
    public void Evaluation_uses_only_the_selected_metric(string unit, decimal hours, decimal points,
        decimal requiredHours, decimal requiredPoints, decimal percent)
        => Assert.Equal(percent, EvaluationMetric.Percent(unit, hours, points, requiredHours, requiredPoints));

    [Theory]
    [InlineData("BOTH", 100, 100)]
    [InlineData("GIO", 0, 100)]
    [InlineData("DIEM", 100, 0)]
    public void Evaluation_requires_a_valid_metric_and_its_own_norm(string unit, decimal hours, decimal points)
        => Assert.Throws<BusinessRuleException>(() => EvaluationMetric.Percent(unit, 100, 100, hours, points));

    [Fact]
    public void Finalized_evaluation_keeps_its_selected_metric()
    {
        var evaluation = new Evaluation(1, 1, 1, 1, "GV", "Giảng viên", 1, null, null, "DIEM");
        evaluation.Finalize("Tốt", "Theo điểm");
        Assert.Equal("DIEM", evaluation.EvaluationUnit);
        Assert.Throws<InvalidOperationException>(() => evaluation.SelectUnit("GIO"));
    }

    [Theory]
    [InlineData(false, false, Roles.DepartmentHead, ProductStatuses.DepartmentHeadApproved)]
    [InlineData(true, false, Roles.FacultyDean, ProductStatuses.FacultyDeanApproved)]
    [InlineData(true, true, Roles.ResearchOffice, ProductStatuses.Approved)]
    public void Submitted_product_skips_conflicted_reviewers(bool head, bool dean, string role, string status)
        => Assert.Equal((role, status), ProductReviewPolicy.Next(ProductStatuses.Submitted, head, dean));

    [Fact]
    public void Dean_conflict_routes_department_approved_product_to_office()
        => Assert.Equal((Roles.ResearchOffice, ProductStatuses.Approved),
            ProductReviewPolicy.Next(ProductStatuses.DepartmentHeadApproved, false, true));

    [Theory]
    [InlineData(null, 1)]
    [InlineData("2026-11-14", 1)]
    [InlineData("2026-11-10", 0)]
    public void Recognition_requires_real_date_and_evidence(string? date, int evidence)
        => Assert.Throws<BusinessRuleException>(() => ProductRecognitionRules.Validate(
            date is null ? null : DateOnly.Parse(date), new DateOnly(2026, 11, 13), "DE_TAI", null, evidence));

    [Fact]
    public void Recognition_accepts_completed_project_and_blocks_unpublished_article()
    {
        var date = new DateOnly(2026, 11, 10);
        ProductRecognitionRules.Validate(date, date, "DE_TAI", null, 1);
        Assert.Throws<BusinessRuleException>(() => ProductRecognitionRules.Validate(date, date, "BAI_BAO", "DANG_PHAN_BIEN", 1));
    }

    [Theory]
    [InlineData(" https://doi.org/10.1000/ABC ", "10.1000/abc")]
    [InlineData("DOI: 10.1000/ABC", "10.1000/abc")]
    [InlineData("  ", null)]
    public void Work_identifier_normalizes_equivalent_doi(string input, string? expected)
    {
        var product = new Product(1, 2, "Công trình", null, null, null);
        product.SetWorkIdentifier(input);
        Assert.Equal(expected, product.WorkIdentifier);
    }

    [Fact]
    public void Legacy_date_repair_cannot_rewrite_an_existing_recognition_date()
    {
        var product = new Product(1, 2, "Công trình", null, null, null);
        Assert.Throws<InvalidOperationException>(() => product.CorrectMissingRecognitionDate(new(2026, 11, 10)));
        product.Approve(ProductStatuses.Approved);
        product.CorrectMissingRecognitionDate(new(2026, 11, 10));
        Assert.Throws<InvalidOperationException>(() => product.CorrectMissingRecognitionDate(new(2026, 11, 11)));
    }

    // Optional PostgreSQL integration suite: run only against a separate disposable database.
    public sealed class PostgresFactAttribute : FactAttribute
    {
        public PostgresFactAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("BUSINESS_TEST_DB")))
                Skip = "Set BUSINESS_TEST_DB to a dedicated database initialized from infra/database scripts.";
        }
    }

    [PostgresFact]
    public async Task PostgreSql_workflow_routes_coauthor_conflicts_prevents_double_credit_and_uses_publication_year()
    {
        var connection = Environment.GetEnvironmentVariable("BUSINESS_TEST_DB")!;
        if (!new Npgsql.NpgsqlConnectionStringBuilder(connection).Database!.StartsWith("research_business_test_"))
            throw new InvalidOperationException("Integration tests require a research_business_test_ database.");
        var user = new TestUser();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = connection,
            ["Seed:SampleData"] = "true",
            ["Jwt:SecretKey"] = "business-tests-only-secret-0123456789abcdef",
            ["Minio:Endpoint"] = "localhost:9000", ["Minio:AccessKey"] = "tests", ["Minio:SecretKey"] = "tests-secret"
        }).Build();
        var services = new ServiceCollection().AddLogging().AddInfrastructure(configuration);
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ICurrentUser>(user);
        await using var provider = services.BuildServiceProvider();
        var tag = Guid.NewGuid().ToString("N")[..8];
        long productId, itemId, lecturerId, oldYearId, publicationYearId, typeId, classificationId;
        Account owner, head, dean, office;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var faculty = new Faculty("F" + tag, "Khoa kiểm thử"); db.Add(faculty); await db.SaveChangesAsync();
            var department = new Department { FacultyId = faculty.Id, Code = "D" + tag, Name = "Bộ môn" };
            db.Add(department); await db.SaveChangesAsync();
            owner = MakeAccount("owner", Roles.Lecturer, Scopes.Personal);
            head = MakeAccount("head", Roles.DepartmentHead, Scopes.Department, faculty.Id, department.Id);
            dean = MakeAccount("dean", Roles.FacultyDean, Scopes.Faculty, faculty.Id);
            office = MakeAccount("office", Roles.ResearchOffice, Scopes.Global);
            db.AddRange(owner, head, dean, office); await db.SaveChangesAsync();
            var lecturer = new Lecturer(department.Id, owner.Id, "G" + tag, "Tác giả", null, null, null, null, null, null, null);
            var headLecturer = new Lecturer(department.Id, head.Id, "H" + tag, "Đồng tác giả TBM", null, null, null, null, null, null, null);
            db.AddRange(lecturer, headLecturer); await db.SaveChangesAsync();
            lecturerId = lecturer.Id;
            var oldYear = new AcademicYear { Code = "O" + tag, StartDate = new(2025, 8, 1), EndDate = new(2026, 7, 31) };
            var currentYear = new AcademicYear { Code = "N" + tag, StartDate = new(2026, 8, 1), EndDate = new(2027, 7, 31) };
            db.AddRange(oldYear, currentYear); await db.SaveChangesAsync();
            oldYearId = oldYear.Id; publicationYearId = currentYear.Id;
            var type = new ProductType("TEST_" + tag.ToUpperInvariant(), "Sản phẩm kiểm thử");
            db.Add(type); await db.SaveChangesAsync(); typeId = type.Id;
            var classification = new ProductClassification { ProductTypeId = typeId, Code = "C" + tag, Name = "Phân loại kiểm thử" };
            db.Add(classification); await db.SaveChangesAsync(); classificationId = classification.Id;
            var plan = new ResearchPlan(lecturer.Id, oldYear.Id, 1, oldYear.StartDate); plan.Submit();
            db.Add(plan); await db.SaveChangesAsync();
            var item = new ResearchPlanItem(plan.Id, typeId, "Nội dung", 1); db.Add(item); await db.SaveChangesAsync(); itemId = item.Id;
            var product = new Product(item.Id, lecturer.Id, "Công trình " + tag, null, null, null);
            product.ClassificationId = classificationId;
            db.Add(product); await db.SaveChangesAsync(); productId = product.Id;
            db.Add(new ProductCoAuthor(product.Id, headLecturer.Id, 1, "DONG_TAC_GIA"));
            await db.SaveChangesAsync();

            Account MakeAccount(string prefix, string role, string roleScope, long? f = null, long? d = null)
            {
                var account = new Account(prefix + tag, "test-hash", prefix, null);
                account.AddRole(new RoleAssignment(role, roleScope, f, d));
                return account;
            }
        }

        async Task<T> WithService<T>(Account actor, Func<ProductService, Task<T>> action)
        {
            user.Use(actor);
            await using var scope = provider.CreateAsyncScope();
            return await action(scope.ServiceProvider.GetRequiredService<ProductService>());
        }
        user.Use(owner);
        ProductResponse submitted;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var controller = new ProductsController(scope.ServiceProvider.GetRequiredService<ProductService>(), db, user, new(db));
            submitted = Assert.IsType<ProductResponse>(Assert.IsType<OkObjectResult>((await controller.Submit(productId, default)).Result).Value);
            Assert.Contains(await db.Notifications.ToListAsync(), n => n.RecipientAccountId == dean.Id && n.ScientificProductId == productId);
            Assert.DoesNotContain(await db.Notifications.ToListAsync(), n => n.RecipientAccountId == head.Id && n.ScientificProductId == productId);
        }
        Assert.Equal(Roles.FacultyDean, submitted.RequiredReviewRole);
        Assert.False(submitted.CanReview);
        user.Use(dean);
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var controller = new ProductsController(scope.ServiceProvider.GetRequiredService<ProductService>(), db, user, new(db));
            var pending = Assert.IsType<OkObjectResult>(await controller.PendingReview(default));
            Assert.Contains($"\"id\":{productId},", System.Text.Json.JsonSerializer.Serialize(pending.Value,
                new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web)));
        }
        await Assert.ThrowsAsync<ForbiddenException>(() => WithService(head,
            s => s.ReviewAsync(productId, new() { Action = "APPROVE" }, default)));
        var reviewed = await WithService(dean, s => s.ReviewAsync(productId, new() { Action = "APPROVE" }, default));
        Assert.Equal(ProductStatuses.FacultyDeanApproved, reviewed.Status);
        await Assert.ThrowsAsync<BusinessRuleException>(() => WithService(office,
            s => s.ReviewAsync(productId, new() { Action = "APPROVE" }, default)));

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var product = await db.Products.FindAsync(productId);
            product!.Update(product.Title, null, null, new(2026, 11, 10)); await db.SaveChangesAsync();
        }
        // Date alone is insufficient: evidence is required too.
        await Assert.ThrowsAsync<BusinessRuleException>(() => WithService(office,
            s => s.ReviewAsync(productId, new() { Action = "APPROVE" }, default)));
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Add(new ProductEvidence(productId, owner.Id, "nghiem-thu.pdf", $"tests/{tag}/evidence.pdf", 100, "Nghiệm thu"));
            await db.SaveChangesAsync();
        }
        var approved = await WithService(office, s => s.ReviewAsync(productId, new() { Action = "APPROVE" }, default));
        Assert.Equal(ProductStatuses.Approved, approved.Status);
        Assert.False(approved.CanReview);

        // Certificates remain available for declaration, even without any conversion rule.
        long certificateItemId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.True(await db.ProductTypes.Where(x => x.Code == "CHUNG_NHAN").Select(x => x.IsActive).SingleAsync());
            var plan = await db.ResearchPlans.SingleAsync(x => x.LecturerId == lecturerId);
            plan.Reopen(); plan.Update(2, null); await db.SaveChangesAsync();
            user.Use(owner);
            var itemService = scope.ServiceProvider.GetRequiredService<ResearchManagement.Application.ResearchPlans.ResearchPlanItemService>();
            var certificateType = await db.ProductTypes.SingleAsync(x => x.Code == "CHUNG_NHAN");
            var item = await itemService.CreateAsync(plan.Id, new() { ProductTypeId = certificateType.Id, Name = "Chứng nhận", CommittedQuantity = 1 }, default);
            certificateItemId = item.Id;
            await scope.ServiceProvider.GetRequiredService<ResearchManagement.Application.ResearchPlans.ResearchPlanService>().SubmitAsync(plan.Id, default);
        }
        var certificate = await WithService(owner, s => s.CreateAsync(new()
        {
            ResearchPlanItemId = certificateItemId, Title = "Chứng nhận " + tag, PublishedDate = new(2026, 11, 10)
        }, default));
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            (await db.Products.FindAsync(certificate.Id))!.Approve(ProductStatuses.Approved);
            await db.SaveChangesAsync();
        }

        long duplicateId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var duplicate = new Product(itemId, lecturerId, "  CÔNG TRÌNH   " + tag.ToUpperInvariant(), null, null, new(2026, 11, 10));
            db.Add(duplicate); await db.SaveChangesAsync(); duplicateId = duplicate.Id;
            var rule = new ConversionRule(typeId, "RULE" + tag, 1, "Giờ", "", 200, "GIO", 100, "", new(2020, 1, 1), null, "Kiểm thử");
            // General group rule must apply to a product that has a classification.
            db.Add(rule); await db.SaveChangesAsync();
            db.Add(new ResearchNorm(lecturerId, publicationYearId, 200, "Test", 100));
            db.Add(new EmulationClassificationRule(publicationYearId, "Tốt", 100, 1));
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<BusinessRuleException>(() => WithService(owner, s => s.SubmitAsync(duplicateId, default)));

        user.Use(office);
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var controller = new EvaluationsController(db, user, new WorkflowNotificationDispatcher(db));
            var created = await controller.Create(new(lecturerId, publicationYearId), default);
            var value = Assert.IsType<CreatedAtActionResult>(created.Result);
            var detail = Assert.IsType<ResearchManagement.Application.Evaluations.Models.EvaluationDetailResponse>(value.Value);
            Assert.Contains(detail.Details, x => x.ProductId == productId);
            Assert.Equal("GIO", detail.EvaluationUnit);
            Assert.All(detail.Details, x => Assert.Equal("GIO", x.Unit));
            Assert.DoesNotContain(detail.Details, x => x.ProductId == certificate.Id);
            Assert.Equal(200m, detail.Details.Single(x => x.ProductId == productId).BaseValue);
            // Point norm remains positive, but an evaluation by hours does not require point conversions.
            await controller.Finalize(detail.Id, new(), default);
            Assert.Equal("CHOT", (await db.Evaluations.FindAsync(detail.Id))!.Status);
            await Assert.ThrowsAsync<BusinessRuleException>(() => controller.Create(new(lecturerId, publicationYearId, "DIEM"), default));
            db.ChangeTracker.Clear();

            var specific = new ConversionRule(typeId, "SPECIFIC_" + tag, 1, "Riêng", "", 240, "GIO", 1, "", new(2020, 1, 1), null, "Kiểm thử") { ClassificationId = classificationId };
            var pointRule = new ConversionRule(typeId, "POINTS_" + tag, 1, "Điểm", "", 100, "DIEM", 100, "", new(2020, 1, 1), null, "Kiểm thử");
            db.AddRange(specific, pointRule); await db.SaveChangesAsync();
            var specificCreated = await controller.Create(new(lecturerId, publicationYearId, "GIO"), default);
            var specificDetail = Assert.IsType<ResearchManagement.Application.Evaluations.Models.EvaluationDetailResponse>(Assert.IsType<CreatedAtActionResult>(specificCreated.Result).Value);
            Assert.Equal(240m, specificDetail.Details.Single(x => x.ProductId == productId).BaseValue);
            // A lower priority specific rule still takes precedence over a general rule.
            Assert.Equal(specific.Id, specificDetail.Details.Single(x => x.ProductId == productId).ConversionRuleId);
            var pointCreated = await controller.Create(new(lecturerId, publicationYearId, "DIEM"), default);
            var pointDetail = Assert.IsType<ResearchManagement.Application.Evaluations.Models.EvaluationDetailResponse>(Assert.IsType<CreatedAtActionResult>(pointCreated.Result).Value);
            Assert.All(pointDetail.Details, x => Assert.Equal("DIEM", x.Unit));
            await controller.Finalize(pointDetail.Id, new(), default);
            Assert.Equal("Tốt", (await db.Evaluations.FindAsync(pointDetail.Id))!.Classification);
            Assert.Equal("DIEM", (await db.Evaluations.FindAsync(pointDetail.Id))!.EvaluationUnit);
            user.Use(owner);
            var progress = Assert.IsType<OkObjectResult>(await new ResearchProgressController(db, user).Mine(publicationYearId, default));
            var progressJson = System.Text.Json.JsonSerializer.SerializeToElement(progress.Value);
            Assert.Equal("DIEM", progressJson.GetProperty("EvaluationUnit").GetString());
            Assert.Equal(100m, progressJson.GetProperty("CompletionPercent").GetDecimal());
            user.Use(office);
            var oldCreated = await controller.Create(new(lecturerId, oldYearId), default);
            var oldDetail = Assert.IsType<ResearchManagement.Application.Evaluations.Models.EvaluationDetailResponse>(Assert.IsType<CreatedAtActionResult>(oldCreated.Result).Value);
            Assert.DoesNotContain(oldDetail.Details, x => x.ProductId == productId);
            var report = await new ReportsController(db, user).Get(new() { AcademicYearId = publicationYearId }, default);
            Assert.Equal(2, report.Value!.Rows.Single(x => x.LecturerId == lecturerId).Products);
            var oldReport = await new ReportsController(db, user).Get(new() { AcademicYearId = oldYearId }, default);
            Assert.Equal(0, oldReport.Value!.Rows.Single(x => x.LecturerId == lecturerId).Products);
        }

        // Legacy approved records can be repaired by QLKH without reopening academic content.
        long legacyId;
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var legacy = new Product(itemId, lecturerId, "Hồ sơ cũ " + tag, null, null, null);
            legacy.Approve(ProductStatuses.Approved);
            db.Add(legacy); await db.SaveChangesAsync(); legacyId = legacy.Id;
        }
        await Assert.ThrowsAsync<ForbiddenException>(() => WithService(owner,
            s => s.CorrectRecognitionDateAsync(legacyId, new(2026, 11, 9), "Bổ sung hồ sơ", default)));
        var repaired = await WithService(office,
            s => s.CorrectRecognitionDateAsync(legacyId, new(2026, 11, 9), "Bổ sung từ biên bản nghiệm thu", default));
        Assert.Equal(new DateOnly(2026, 11, 9), repaired.PublishedDate);
        Assert.Equal(ProductStatuses.Approved, repaired.Status);
        await Assert.ThrowsAsync<BusinessRuleException>(() => WithService(office,
            s => s.CorrectRecognitionDateAsync(legacyId, new(2026, 11, 8), "Không được đổi ngày đã có", default)));

        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Contains(await db.ReviewHistories.Where(x => x.ProductId == legacyId).ToListAsync(),
                x => x.Comment != null && x.Comment.Contains("biên bản nghiệm thu"));
            Assert.Equal(new DateOnly(2026, 11, 9), (await db.ProductSnapshots.Where(x => x.ProductId == legacyId).SingleAsync()).PublishedDate);

            // Equivalent DOI forms cannot create separate physical records, including concurrent callers.
            var first = new Product(itemId, lecturerId, "DOI A " + tag, null, null, null);
            first.SetWorkIdentifier("https://doi.org/10.1000/" + tag);
            db.Add(first); await db.SaveChangesAsync();
            var second = new Product(itemId, lecturerId, "DOI B " + tag, null, null, null);
            second.SetWorkIdentifier("DOI:10.1000/" + tag.ToUpperInvariant());
            db.Add(second);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }

        // A normal restart must preserve user data once demo data already exists.
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await db.Accounts.AnyAsync(x => x.Username == "qlkh"))
                db.Add(new Account("qlkh", "test-hash", "Dữ liệu mẫu đã tồn tại", null));
            await db.SaveChangesAsync();
        }
        await ResearchManagement.Infrastructure.Persistence.Seed.DemoDataSeeder.SeedAsync(provider);
        var afterRestart = await WithService(owner, s => s.GetByIdAsync(productId, default));
        Assert.Equal(ProductStatuses.Approved, afterRestart.Status);
        Assert.Single(afterRestart.CoAuthors);
        Assert.Single(afterRestart.Evidences);

        // Upgrade restores certificates once, then respects subsequent administrator choices.
        await using (var scope = provider.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("DELETE FROM nckh.ung_dung_migration WHERE ma = '18_project_requirements'");
            await db.Database.ExecuteSqlRawAsync("UPDATE nckh.loai_san_pham SET dang_su_dung = false WHERE ma_loai = 'CHUNG_NHAN'");
            await using var script = typeof(EvaluationsController).Assembly.GetManifestResourceStream("ProjectRequirementsMigration.sql")!;
            using var reader = new StreamReader(script);
            var sql = await reader.ReadToEndAsync();
            await db.Database.ExecuteSqlRawAsync(sql);
            Assert.True(await db.ProductTypes.AsNoTracking().Where(x => x.Code == "CHUNG_NHAN").Select(x => x.IsActive).SingleAsync());
            await db.Database.ExecuteSqlRawAsync("UPDATE nckh.loai_san_pham SET dang_su_dung = false WHERE ma_loai = 'CHUNG_NHAN'");
            await db.Database.ExecuteSqlRawAsync(sql);
            Assert.False(await db.ProductTypes.AsNoTracking().Where(x => x.Code == "CHUNG_NHAN").Select(x => x.IsActive).SingleAsync());
            Assert.Contains(await db.Evaluations.AsNoTracking().Where(x => x.LecturerId == lecturerId && x.Status == "CHOT").ToListAsync(),
                x => x.EvaluationUnit == "DIEM" && x.Classification == "Tốt");
            // Roll back only this migration scenario to leave the test fixture reusable.
            await transaction.RollbackAsync();
        }
    }

    private sealed class TestUser : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public long? Id { get; private set; }
        public IReadOnlyList<RoleAssignmentDto> Assignments { get; private set; } = [];
        public bool IsInRole(string role) => Assignments.Any(x => x.Role == role);
        public bool CanAccess(long? faculty, long? department) => ScopeRules.CanAccess(Assignments, faculty, department);
        public void Use(Account account)
        {
            Id = account.Id;
            Assignments = account.RoleAssignments.Select(x => new RoleAssignmentDto(x.Role, x.Scope, x.FacultyId, x.DepartmentId)).ToList();
        }
    }
}
