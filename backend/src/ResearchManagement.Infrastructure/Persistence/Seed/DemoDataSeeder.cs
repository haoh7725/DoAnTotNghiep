using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Dữ liệu minh họa xuyên suốt nghiệp vụ, chỉ dùng cho môi trường phát triển.
/// Seeder chạy lặp lại an toàn: nếu tài khoản đánh dấu demo.qlkh đã tồn tại thì bỏ qua.
/// </summary>
public static class DemoDataSeeder
{
    public const string DefaultPassword = "Demo@123456";
    private const string MarkerUsername = "demo.qlkh";

    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemoDataSeeder");

        if (!configuration.GetValue("Seed:DemoData", false))
        {
            logger.LogInformation("Bỏ qua dữ liệu demo: Seed:DemoData không được bật.");
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        if (await db.Accounts.AnyAsync(x => x.Username == MarkerUsername, cancellationToken))
        {
            logger.LogInformation("Dữ liệu demo đã tồn tại, bỏ qua.");
            return;
        }

        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var faculty = new Faculty("DEMO_CNTT", "Khoa Công nghệ Thông tin (Demo)");
            db.Faculties.Add(faculty);
            await db.SaveChangesAsync(cancellationToken);

            var softwareDepartment = new Department
            {
                FacultyId = faculty.Id,
                Code = "DEMO_CNPM",
                Name = "Bộ môn Công nghệ phần mềm"
            };
            var informationSystemsDepartment = new Department
            {
                FacultyId = faculty.Id,
                Code = "DEMO_HTTT",
                Name = "Bộ môn Hệ thống thông tin"
            };
            db.Departments.AddRange(softwareDepartment, informationSystemsDepartment);

            var academicYear = new AcademicYear
            {
                Code = "DEMO-2026-2027",
                StartDate = new DateOnly(2026, 8, 1),
                EndDate = new DateOnly(2027, 7, 31)
            };
            db.AcademicYears.Add(academicYear);
            await db.SaveChangesAsync(cancellationToken);

            var researchOffice = CreateAccount(MarkerUsername, "Nguyễn Thu Hà", "qlkh@demo.local",
                Roles.ResearchOffice, Scopes.Global);
            var board = CreateAccount("demo.bgh", "PGS. TS. Trần Minh Quân", "bgh@demo.local",
                Roles.BoardOfDirectors, Scopes.Global);
            var dean = CreateAccount("demo.truongkhoa", "PGS. TS. Lê Hoàng Nam", "truongkhoa@demo.local",
                Roles.FacultyDean, Scopes.Faculty, faculty.Id);
            var head = CreateAccount("demo.truongbomon", "TS. Phạm Đức Anh", "truongbomon@demo.local",
                Roles.DepartmentHead, Scopes.Department, faculty.Id, softwareDepartment.Id);
            var lecturer1 = CreateAccount("demo.gv01", "TS. Nguyễn Minh Anh", "gv01@demo.local",
                Roles.Lecturer, Scopes.Personal);
            var lecturer2 = CreateAccount("demo.gv02", "ThS. Trần Ngọc Lan", "gv02@demo.local",
                Roles.Lecturer, Scopes.Personal);
            var lecturer3 = CreateAccount("demo.gv03", "TS. Võ Thành Công", "gv03@demo.local",
                Roles.Lecturer, Scopes.Personal);
            var lecturer4 = CreateAccount("demo.gv04", "ThS. Đặng Hải Yến", "gv04@demo.local",
                Roles.Lecturer, Scopes.Personal);
            db.Accounts.AddRange(researchOffice, board, dean, head, lecturer1, lecturer2, lecturer3, lecturer4);
            await db.SaveChangesAsync(cancellationToken);

            var lecturers = new[]
            {
                new Lecturer(softwareDepartment.Id, lecturer1.Id, "GV001", "TS. Nguyễn Minh Anh",
                    new DateOnly(1985, 3, 12), "NỮ", "gv01@demo.local", "0901000001", null, "TIẾN SĨ", "Giảng viên chính"),
                new Lecturer(softwareDepartment.Id, lecturer2.Id, "GV002", "ThS. Trần Ngọc Lan",
                    new DateOnly(1990, 8, 21), "NỮ", "gv02@demo.local", "0901000002", null, "THẠC SĨ", "Giảng viên"),
                new Lecturer(informationSystemsDepartment.Id, lecturer3.Id, "GV003", "TS. Võ Thành Công",
                    new DateOnly(1983, 11, 5), "NAM", "gv03@demo.local", "0901000003", "PGS", "TIẾN SĨ", "Giảng viên cao cấp"),
                new Lecturer(informationSystemsDepartment.Id, lecturer4.Id, "GV004", "ThS. Đặng Hải Yến",
                    new DateOnly(1992, 6, 17), "NỮ", "gv04@demo.local", "0901000004", null, "THẠC SĨ", "Giảng viên")
            };
            db.Lecturers.AddRange(lecturers);
            await db.SaveChangesAsync(cancellationToken);

            db.ScientificProfiles.AddRange(
                new ScientificProfile(lecturers[0].Id, "Kỹ thuật phần mềm", "Trí tuệ nhân tạo",
                    "Ứng dụng học máy trong giáo dục", "Chủ nhiệm 2 đề tài và công bố 8 bài báo.", DateTimeOffset.UtcNow),
                new ScientificProfile(lecturers[1].Id, "Kiểm thử phần mềm", "Đảm bảo chất lượng",
                    "Tự động hóa kiểm thử", "Tham gia nhóm nghiên cứu SE Lab.", DateTimeOffset.UtcNow),
                new ScientificProfile(lecturers[2].Id, "Hệ thống thông tin", "Khoa học dữ liệu",
                    "Kho dữ liệu và phân tích học tập", "Có công bố quốc tế thuộc danh mục Scopus.", DateTimeOffset.UtcNow),
                new ScientificProfile(lecturers[3].Id, "Cơ sở dữ liệu", "Chuyển đổi số",
                    "Hệ thống thông tin quản lý", "Nghiên cứu các nền tảng quản trị đại học.", DateTimeOffset.UtcNow));

            db.ResearchNorms.AddRange(lecturers.Select((lecturer, index) =>
                new ResearchNorm(lecturer.Id, academicYear.Id, index is 0 or 2 ? 280 : 200,
                    "Định mức NCKH năm học 2026-2027")));

            var target = new ResearchTarget(faculty.Id, academicYear.Id, 12, new DateOnly(2027, 6, 30));
            db.ResearchTargets.Add(target);
            await db.SaveChangesAsync(cancellationToken);
            db.ResearchTargetAllocations.AddRange(
                new ResearchTargetAllocation(target.Id, faculty.Id, softwareDepartment.Id, 7, new DateOnly(2026, 9, 1)),
                new ResearchTargetAllocation(target.Id, faculty.Id, informationSystemsDepartment.Id, 5, new DateOnly(2026, 9, 1)));

            var productTypes = await EnsureProductTypesAsync(db, cancellationToken);
            var plans = lecturers.Select((lecturer, index) =>
            {
                var plan = new ResearchPlan(lecturer.Id, academicYear.Id, 2,
                    new DateOnly(2026, 9, 5 + index), new DateOnly(2027, 6, 30));
                if (index != 3) plan.Submit();
                return plan;
            }).ToArray();
            db.ResearchPlans.AddRange(plans);
            await db.SaveChangesAsync(cancellationToken);

            var items = new[]
            {
                new ResearchPlanItem(plans[0].Id, productTypes["BAI_BAO"].Id, "Bài báo về dự báo kết quả học tập", 1, new DateOnly(2027, 3, 31)),
                new ResearchPlanItem(plans[0].Id, productTypes["DE_TAI"].Id, "Đề tài xây dựng trợ lý học tập thông minh", 1, new DateOnly(2027, 6, 30)),
                new ResearchPlanItem(plans[1].Id, productTypes["BAI_BAO"].Id, "Bài báo về kiểm thử tự động", 1, new DateOnly(2026, 11, 15)),
                new ResearchPlanItem(plans[1].Id, productTypes["CHUNG_NHAN"].Id, "Đăng ký chứng nhận phần mềm", 1, new DateOnly(2027, 5, 15)),
                new ResearchPlanItem(plans[2].Id, productTypes["BAI_BAO"].Id, "Bài báo phân tích dữ liệu giáo dục", 1, new DateOnly(2027, 2, 28)),
                new ResearchPlanItem(plans[2].Id, productTypes["SACH"].Id, "Giáo trình kho dữ liệu", 1, new DateOnly(2027, 6, 1)),
                new ResearchPlanItem(plans[3].Id, productTypes["DE_TAI"].Id, "Đề tài chuyển đổi số cấp khoa", 1, new DateOnly(2027, 4, 30))
            };
            items[0].UpdateProgress("DANG_THUC_HIEN", "Đã hoàn thành bản thảo", null);
            items[2].UpdateProgress("DANG_THUC_HIEN", "Đã có kết quả thử nghiệm", "Cần bổ sung bộ dữ liệu kiểm thử");
            items[4].UpdateProgress("DA_THUC_HIEN", "Đã công bố", null);
            db.ResearchPlanItems.AddRange(items);
            await db.SaveChangesAsync(cancellationToken);

            db.ProgressHistories.AddRange(
                new ProgressHistory(items[0].Id, lecturer1.Id, "DANG_THUC_HIEN", "Đã hoàn thành bản thảo", null),
                new ProgressHistory(items[2].Id, lecturer2.Id, "DANG_THUC_HIEN", "Đã có kết quả thử nghiệm", "Cần bổ sung bộ dữ liệu kiểm thử"),
                new ProgressHistory(items[4].Id, lecturer3.Id, "DA_THUC_HIEN", "Đã công bố", null));

            var products = new[]
            {
                NewArticle(items[0], lecturers[0], "Mô hình dự báo nguy cơ học vụ bằng học máy", "Tạp chí Khoa học Giáo dục", "SCOPUS", "Q3"),
                NewProject(items[1], lecturers[0], "Trợ lý học tập thông minh cho sinh viên", "CẤP_TRƯỜNG"),
                NewArticle(items[2], lecturers[1], "Khung kiểm thử hồi quy tự động cho ứng dụng Web", "Kỷ yếu FAIR 2026", null, null),
                NewArticle(items[4], lecturers[2], "Learning Analytics Dashboard for Higher Education", "Journal of Educational Data", "SCOPUS", "Q2"),
                NewProject(items[6], lecturers[3], "Nền tảng số hóa quy trình quản lý cấp khoa", "CẤP_KHOA"),
                NewArticle(items[5], lecturers[2], "Kiến trúc kho dữ liệu phục vụ quản trị đại học", "Tạp chí Khoa học và Công nghệ", null, null),
                NewArticle(items[3], lecturers[1], "A Practical Model for Software Quality Assurance", "ICT Proceedings 2026", "SCOPUS", null)
            };

            // NHAP, NOP, TRUONG_BO_MON_DUYET, DA_DUYET và TRA_LAI.
            products[1].Submit();
            products[2].Submit();
            products[2].Approve(ProductStatuses.DepartmentHeadApproved);
            products[3].Submit();
            products[3].Approve(ProductStatuses.DepartmentHeadApproved);
            products[3].Approve(ProductStatuses.FacultyDeanApproved);
            products[3].Approve(ProductStatuses.ResearchOfficeApproved);
            products[3].Approve(ProductStatuses.Approved, 120m);
            products[4].Submit();
            products[4].Return();
            products[5].Submit();
            products[5].Approve(ProductStatuses.DepartmentHeadApproved);
            products[5].Approve(ProductStatuses.FacultyDeanApproved);
            products[6].Submit();
            products[6].Approve(ProductStatuses.DepartmentHeadApproved);
            products[6].Approve(ProductStatuses.FacultyDeanApproved);
            products[6].Approve(ProductStatuses.ResearchOfficeApproved);
            db.Products.AddRange(products);
            await db.SaveChangesAsync(cancellationToken);

            db.ProductCoAuthors.AddRange(
                new ProductCoAuthor(products[0].Id, lecturers[1].Id, 2),
                new ProductCoAuthor(products[3].Id, lecturers[0].Id, 2));
            db.ProductEvidences.AddRange(
                await UploadDemoEvidenceAsync(products[1].Id, lecturer1.Id,
                    "mau-thuyet-minh-de-tai.pdf", "Mẫu thuyết minh đề tài KH&CN công khai"),
                await UploadDemoEvidenceAsync(products[3].Id, lecturer3.Id,
                    "gritnet-student-performance.pdf", "Bài báo khoa học công khai từ arXiv"));
            db.ReviewHistories.AddRange(
                new ReviewHistory(products[1].Id, lecturer1.Id, ProductStatuses.Draft, ProductStatuses.Submitted, "Nộp xét duyệt"),
                new ReviewHistory(products[2].Id, lecturer2.Id, ProductStatuses.Draft, ProductStatuses.Submitted, "Nộp xét duyệt"),
                new ReviewHistory(products[2].Id, head.Id, ProductStatuses.Submitted, ProductStatuses.DepartmentHeadApproved, "Nội dung đạt yêu cầu bộ môn"),
                new ReviewHistory(products[3].Id, lecturer3.Id, ProductStatuses.Draft, ProductStatuses.Submitted, "Nộp xét duyệt"),
                new ReviewHistory(products[3].Id, head.Id, ProductStatuses.Submitted, ProductStatuses.DepartmentHeadApproved, "Đã kiểm tra minh chứng"),
                new ReviewHistory(products[3].Id, dean.Id, ProductStatuses.DepartmentHeadApproved, ProductStatuses.FacultyDeanApproved, "Khoa thống nhất đề nghị công nhận"),
                new ReviewHistory(products[3].Id, researchOffice.Id, ProductStatuses.FacultyDeanApproved, ProductStatuses.ResearchOfficeApproved, "Minh chứng hợp lệ"),
                new ReviewHistory(products[3].Id, researchOffice.Id, ProductStatuses.ResearchOfficeApproved, ProductStatuses.Approved, "Công nhận 120 giờ quy đổi"),
                new ReviewHistory(products[4].Id, researchOffice.Id, ProductStatuses.Submitted, ProductStatuses.Returned, "Bổ sung quyết định giao đề tài"));

            db.Notifications.AddRange(
                Sent(new Notification(lecturer2.Id, "QUA_HAN", "Nội dung kế hoạch kiểm thử tự động đã quá hạn.",
                    "demo:overdue:item3", items[2].Id, researchPlanId: plans[1].Id)),
                new Notification(lecturer1.Id, "NHAC_HAN", "Bài báo dự báo kết quả học tập sắp đến hạn.",
                    "demo:due:item1", items[0].Id, researchPlanId: plans[0].Id),
                new Notification(lecturer4.Id, "PHAN_HOI", "Sản phẩm đã được trả lại: bổ sung quyết định giao đề tài.",
                    "demo:return:product5", scientificProductId: products[4].Id));

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Đã tạo dữ liệu demo thành công.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        Account CreateAccount(string username, string fullName, string email, string role, string accountScope,
            long? facultyId = null, long? departmentId = null)
        {
            var account = new Account(username, string.Empty, fullName, email);
            account.SetPasswordHash(passwordService.Hash(account, DefaultPassword));
            account.AddRole(new RoleAssignment(role, accountScope, facultyId, departmentId));
            return account;
        }

        async Task<ProductEvidence> UploadDemoEvidenceAsync(
            long productId, long accountId, string fileName, string description)
        {
            var sourcePath = Path.Combine(environment.ContentRootPath, "DemoEvidence", fileName);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Không tìm thấy tệp minh chứng demo đóng gói cùng API.", sourcePath);

            var objectKey = $"uploads/products/{productId}/{fileName}";
            var info = new FileInfo(sourcePath);
            await using var stream = File.OpenRead(sourcePath);
            await fileStorage.PutAsync(
                objectKey, stream, info.Length, "application/pdf", cancellationToken);

            return new ProductEvidence(productId, accountId, fileName, objectKey,
                info.Length, description);
        }
    }

    private static Product NewArticle(ResearchPlanItem item, Lecturer lecturer, string title,
        string publication, string? index, string? classification) =>
        new(item.Id, lecturer.Id, title, "Dữ liệu minh họa phục vụ kiểm thử nghiệp vụ.", publication,
            new DateOnly(2026, 10, 15), "DA_XUAT_BAN", index, classification);

    private static Product NewProject(ResearchPlanItem item, Lecturer lecturer, string title, string level) =>
        new(item.Id, lecturer.Id, title, "Dữ liệu minh họa phục vụ kiểm thử nghiệp vụ.", null,
            null, projectLevel: level);

    private static Notification Sent(Notification notification)
    {
        notification.MarkAsSent();
        return notification;
    }

    private static async Task<Dictionary<string, ProductType>> EnsureProductTypesAsync(
        ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var types = await db.ProductTypes.ToDictionaryAsync(x => x.Code, cancellationToken);
        var defaults = new Dictionary<string, string>
        {
            ["BAI_BAO"] = "Bài báo",
            ["DE_TAI"] = "Đề tài",
            ["SACH"] = "Sách",
            ["CHUNG_NHAN"] = "Chứng nhận"
        };
        foreach (var (code, name) in defaults.Where(x => !types.ContainsKey(x.Key)))
        {
            var type = new ProductType(code, name);
            db.ProductTypes.Add(type);
            types[code] = type;
        }
        await db.SaveChangesAsync(cancellationToken);
        return types;
    }
}
