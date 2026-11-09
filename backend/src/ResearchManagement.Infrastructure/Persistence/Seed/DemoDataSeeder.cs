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
/// Bộ dữ liệu mẫu quy mô khoa, nhất quán xuyên suốt nghiệp vụ, chỉ dùng cho môi trường phát triển.
/// Seeder chạy lặp lại an toàn: nếu tài khoản đánh dấu qlkh đã tồn tại thì chỉ bổ sung phần còn thiếu.
/// </summary>
public static class DemoDataSeeder
{
    public const string DefaultPassword = "Huit@2026!";
    private const string MarkerUsername = "qlkh";

    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DemoDataSeeder");

        // Giữ tương thích tạm thời với Seed:DemoData cho máy phát triển cũ.
        var sampleDataEnabled = configuration.GetValue("Seed:SampleData",
            configuration.GetValue("Seed:DemoData", false));
        if (!sampleDataEnabled)
        {
            logger.LogInformation("Bỏ qua dữ liệu mẫu: Seed:DemoData không được bật.");
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var existingMarker = await db.Accounts.SingleOrDefaultAsync(
            x => x.Username == MarkerUsername, cancellationToken);
        if (existingMarker is not null)
        {
            await using var supplementTransaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await EnsureExtendedDemoDataAsync(db, existingMarker.Id, cancellationToken);
            await EnsureLargeSampleDataAsync(db, passwordService, cancellationToken);
            await EnsureLargeReportEvaluationsAsync(db, existingMarker.Id, cancellationToken);
            await supplementTransaction.CommitAsync(cancellationToken);
            logger.LogInformation("Đã kiểm tra và bổ sung dữ liệu mẫu còn thiếu.");
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var faculty = new Faculty("CNTT", "Khoa Công nghệ Thông tin");
            db.Faculties.Add(faculty);
            await db.SaveChangesAsync(cancellationToken);

            var softwareDepartment = new Department
            {
                FacultyId = faculty.Id,
                Code = "CNPM",
                Name = "Bộ môn Công nghệ phần mềm"
            };
            var informationSystemsDepartment = new Department
            {
                FacultyId = faculty.Id,
                Code = "HTTT",
                Name = "Bộ môn Hệ thống thông tin"
            };
            var computerScienceDepartment = new Department
            {
                FacultyId = faculty.Id,
                Code = "KHMT",
                Name = "Bộ môn Khoa học máy tính"
            };
            db.Departments.AddRange(softwareDepartment, informationSystemsDepartment, computerScienceDepartment);

            var academicYear = new AcademicYear
            {
                Code = "2026-2027",
                StartDate = new DateOnly(2026, 8, 1),
                EndDate = new DateOnly(2027, 7, 31)
            };
            db.AcademicYears.Add(academicYear);
            await db.SaveChangesAsync(cancellationToken);

            var researchOffice = CreateAccount(MarkerUsername, "Nguyễn Thu Hà", "qlkh@huit.edu.vn",
                Roles.ResearchOffice, Scopes.Global);
            var board = CreateAccount("bgh", "PGS. TS. Trần Minh Quân", "bgh@huit.edu.vn",
                Roles.BoardOfDirectors, Scopes.Global);
            var dean = CreateAccount("truongkhoa", "PGS. TS. Lê Hoàng Nam", "truongkhoa@huit.edu.vn",
                Roles.FacultyDean, Scopes.Faculty, faculty.Id);
            var head = CreateAccount("tbm.cnpm", "TS. Phạm Đức Anh", "tbm.cnpm@huit.edu.vn",
                Roles.DepartmentHead, Scopes.Department, faculty.Id, softwareDepartment.Id);
            var informationSystemsHead = CreateAccount("tbm.httt", "TS. Lê Quốc Bảo", "tbm.httt@huit.edu.vn",
                Roles.DepartmentHead, Scopes.Department, faculty.Id, informationSystemsDepartment.Id);
            var lecturer1 = CreateAccount("gv.nguyenminhanh", "TS. Nguyễn Minh Anh", "nguyenminhanh@huit.edu.vn",
                Roles.Lecturer, Scopes.Personal);
            var lecturer2 = CreateAccount("gv.tranngoclan", "ThS. Trần Ngọc Lan", "tranngoclan@huit.edu.vn",
                Roles.Lecturer, Scopes.Personal);
            var lecturer3 = CreateAccount("gv.vothanhcong", "TS. Võ Thành Công", "vothanhcong@huit.edu.vn",
                Roles.Lecturer, Scopes.Personal);
            var lecturer4 = CreateAccount("gv.danghaiyen", "ThS. Đặng Hải Yến", "danghaiyen@huit.edu.vn",
                Roles.Lecturer, Scopes.Personal);
            var lecturer5 = CreateAccount("gv.lethanhson", "TS. Lê Thanh Sơn", "lethanhson@huit.edu.vn",
                Roles.Lecturer, Scopes.Personal);
            var lecturer6 = CreateAccount("gv.phamthuytrang", "ThS. Phạm Thùy Trang", "phamthuytrang@huit.edu.vn",
                Roles.Lecturer, Scopes.Personal);
            var lecturer7 = CreateAccount("gv.nguyenkhanhlinh", "TS. Nguyễn Khánh Linh", "nguyenkhanhlinh@huit.edu.vn",
                Roles.Lecturer, Scopes.Personal);
            var lecturer8 = CreateAccount("gv.buiducviet", "ThS. Bùi Đức Việt", "buiducviet@huit.edu.vn",
                Roles.Lecturer, Scopes.Personal);
            db.Accounts.AddRange(researchOffice, board, dean, head, informationSystemsHead,
                lecturer1, lecturer2, lecturer3, lecturer4, lecturer5, lecturer6, lecturer7, lecturer8);
            await db.SaveChangesAsync(cancellationToken);

            var lecturers = new[]
            {
                new Lecturer(softwareDepartment.Id, lecturer1.Id, "CNTT001", "TS. Nguyễn Minh Anh",
                    new DateOnly(1985, 3, 12), "NỮ", "nguyenminhanh@huit.edu.vn", "0901000001", null, "TIẾN SĨ", "Giảng viên chính"),
                new Lecturer(softwareDepartment.Id, lecturer2.Id, "CNTT002", "ThS. Trần Ngọc Lan",
                    new DateOnly(1990, 8, 21), "NỮ", "tranngoclan@huit.edu.vn", "0901000002", null, "THẠC SĨ", "Giảng viên"),
                new Lecturer(informationSystemsDepartment.Id, lecturer3.Id, "CNTT003", "TS. Võ Thành Công",
                    new DateOnly(1983, 11, 5), "NAM", "vothanhcong@huit.edu.vn", "0901000003", "PGS", "TIẾN SĨ", "Giảng viên cao cấp"),
                new Lecturer(informationSystemsDepartment.Id, lecturer4.Id, "CNTT004", "ThS. Đặng Hải Yến",
                    new DateOnly(1992, 6, 17), "NỮ", "danghaiyen@huit.edu.vn", "0901000004", null, "THẠC SĨ", "Giảng viên")
                ,new Lecturer(softwareDepartment.Id, lecturer5.Id, "CNTT005", "TS. Lê Thanh Sơn",
                    new DateOnly(1987, 2, 10), "NAM", "lethanhson@huit.edu.vn", "0901000005", null, "TIẾN SĨ", "Giảng viên chính")
                ,new Lecturer(informationSystemsDepartment.Id, lecturer6.Id, "CNTT006", "ThS. Phạm Thùy Trang",
                    new DateOnly(1991, 9, 8), "NỮ", "phamthuytrang@huit.edu.vn", "0901000006", null, "THẠC SĨ", "Giảng viên")
                ,new Lecturer(computerScienceDepartment.Id, lecturer7.Id, "CNTT007", "TS. Nguyễn Khánh Linh",
                    new DateOnly(1986, 5, 23), "NỮ", "nguyenkhanhlinh@huit.edu.vn", "0901000007", null, "TIẾN SĨ", "Giảng viên chính")
                ,new Lecturer(computerScienceDepartment.Id, lecturer8.Id, "CNTT008", "ThS. Bùi Đức Việt",
                    new DateOnly(1993, 12, 4), "NAM", "buiducviet@huit.edu.vn", "0901000008", null, "THẠC SĨ", "Giảng viên")
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
                    "Hệ thống thông tin quản lý", "Nghiên cứu các nền tảng quản trị đại học.", DateTimeOffset.UtcNow),
                new ScientificProfile(lecturers[4].Id, "Kỹ nghệ phần mềm", "DevOps và kiến trúc phần mềm",
                    "Bảo đảm chất lượng phần mềm", "Công bố và chuyển giao giải pháp quản lý chất lượng phần mềm.", DateTimeOffset.UtcNow),
                new ScientificProfile(lecturers[5].Id, "Phân tích nghiệp vụ", "Hệ thống hỗ trợ quyết định",
                    "Chuyển đổi số doanh nghiệp", "Nghiên cứu khai thác dữ liệu phục vụ quản trị.", DateTimeOffset.UtcNow),
                new ScientificProfile(lecturers[6].Id, "Trí tuệ nhân tạo", "Thị giác máy tính",
                    "AI ứng dụng", "Chủ nhiệm nhóm nghiên cứu AI và dữ liệu lớn.", DateTimeOffset.UtcNow),
                new ScientificProfile(lecturers[7].Id, "Mạng máy tính", "An toàn thông tin",
                    "Hạ tầng số", "Nghiên cứu bảo mật hệ thống và dịch vụ số.", DateTimeOffset.UtcNow));

            db.ResearchNorms.AddRange(lecturers.Select((lecturer, index) =>
                new ResearchNorm(lecturer.Id, academicYear.Id, index is 0 or 2 ? 280 : 200,
                    "Định mức NCKH năm học 2026-2027")));

            var target = new ResearchTarget(faculty.Id, academicYear.Id, 12, new DateOnly(2027, 6, 30));
            target.MarkAssigned(researchOffice.Id);
            target.Accept(dean.Id, DateTimeOffset.UtcNow);
            target.Publish(DateTimeOffset.UtcNow);
            db.ResearchTargets.Add(target);
            await db.SaveChangesAsync(cancellationToken);
            var departmentAllocations = new[]
            {
                new ResearchTargetAllocation(target.Id, faculty.Id, softwareDepartment.Id, 4, new DateOnly(2026, 9, 1)),
                new ResearchTargetAllocation(target.Id, faculty.Id, informationSystemsDepartment.Id, 4, new DateOnly(2026, 9, 1)),
                new ResearchTargetAllocation(target.Id, faculty.Id, computerScienceDepartment.Id, 4, new DateOnly(2026, 9, 1))
            };
            db.ResearchTargetAllocations.AddRange(departmentAllocations);
            await db.SaveChangesAsync(cancellationToken);
            db.LecturerTargetAllocations.AddRange(
                new LecturerTargetAllocation(departmentAllocations[0].Id, lecturers[0].Id, 2, new DateOnly(2026, 9, 2)),
                new LecturerTargetAllocation(departmentAllocations[0].Id, lecturers[1].Id, 1, new DateOnly(2026, 9, 2)),
                new LecturerTargetAllocation(departmentAllocations[0].Id, lecturers[4].Id, 1, new DateOnly(2026, 9, 2)),
                new LecturerTargetAllocation(departmentAllocations[1].Id, lecturers[2].Id, 2, new DateOnly(2026, 9, 2)),
                new LecturerTargetAllocation(departmentAllocations[1].Id, lecturers[3].Id, 1, new DateOnly(2026, 9, 2)),
                new LecturerTargetAllocation(departmentAllocations[1].Id, lecturers[5].Id, 1, new DateOnly(2026, 9, 2)),
                new LecturerTargetAllocation(departmentAllocations[2].Id, lecturers[6].Id, 2, new DateOnly(2026, 9, 2)),
                new LecturerTargetAllocation(departmentAllocations[2].Id, lecturers[7].Id, 2, new DateOnly(2026, 9, 2)));

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
                new ResearchPlanItem(plans[3].Id, productTypes["DE_TAI"].Id, "Đề tài chuyển đổi số cấp khoa", 1, new DateOnly(2027, 4, 30)),
                new ResearchPlanItem(plans[4].Id, productTypes["BAI_BAO"].Id, "Nghiên cứu áp dụng DevOps trong phát triển phần mềm", 1, new DateOnly(2027, 5, 15)),
                new ResearchPlanItem(plans[5].Id, productTypes["BAI_BAO"].Id, "Mô hình hỗ trợ quyết định cho tuyển sinh đại học", 1, new DateOnly(2027, 4, 30)),
                new ResearchPlanItem(plans[6].Id, productTypes["BAI_BAO"].Id, "Nhận diện hành vi học tập từ dữ liệu lớp học", 1, new DateOnly(2027, 3, 15)),
                new ResearchPlanItem(plans[7].Id, productTypes["CHUNG_NHAN"].Id, "Chứng nhận giải pháp bảo mật hệ thống", 1, new DateOnly(2027, 6, 15))
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
                NewArticle(items[3], lecturers[1], "A Practical Model for Software Quality Assurance", "ICT Proceedings 2026", "SCOPUS", null),
                NewArticle(items[7], lecturers[4], "DevOps Adoption Model for University Software Projects", "International Conference on ICT", "SCOPUS", "Q3"),
                NewArticle(items[8], lecturers[5], "Decision Support Model for Higher Education Admission", "Tạp chí Khoa học Giáo dục Việt Nam", "TRONG_NUOC", "UY_TIN"),
                NewArticle(items[9], lecturers[6], "Learning Behavior Recognition from Classroom Data", "Journal of Educational Data", "SCOPUS", "Q2"),
                NewProject(items[10], lecturers[7], "Giải pháp giám sát an toàn hạ tầng số", "CẤP_TRƯỜNG")
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
            products[7].Submit();
            products[7].Approve(ProductStatuses.DepartmentHeadApproved);
            products[8].Submit();
            products[9].Submit();
            products[9].Approve(ProductStatuses.DepartmentHeadApproved);
            products[9].Approve(ProductStatuses.FacultyDeanApproved);
            products[9].Approve(ProductStatuses.ResearchOfficeApproved);
            products[9].Approve(ProductStatuses.Approved, 120m);
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
                    "seed:overdue:item3", items[2].Id, researchPlanId: plans[1].Id)),
                new Notification(lecturer1.Id, "NHAC_HAN", "Bài báo dự báo kết quả học tập sắp đến hạn.",
                    "seed:due:item1", items[0].Id, researchPlanId: plans[0].Id),
                new Notification(lecturer4.Id, "PHAN_HOI", "Sản phẩm đã được trả lại: bổ sung quyết định giao đề tài.",
                    "seed:return:product5", researchPlanItemId: items[6].Id, researchPlanId: plans[3].Id));

            await db.SaveChangesAsync(cancellationToken);
            await EnsureExtendedDemoDataAsync(db, researchOffice.Id, cancellationToken);
            await EnsureLargeSampleDataAsync(db, passwordService, cancellationToken);
            await EnsureLargeReportEvaluationsAsync(db, researchOffice.Id, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            logger.LogInformation("Đã tạo dữ liệu mẫu nghiệp vụ thành công.");
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
                throw new FileNotFoundException("Không tìm thấy tệp minh chứng mẫu đóng gói cùng API.", sourcePath);

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
        new(item.Id, lecturer.Id, title, "Sản phẩm được khởi tạo để mô phỏng hồ sơ NCKH theo quy trình quản lý của khoa.", publication,
            new DateOnly(2026, 10, 15), null, index, classification);

    private static Product NewProject(ResearchPlanItem item, Lecturer lecturer, string title, string level) =>
        new(item.Id, lecturer.Id, title, "Sản phẩm được khởi tạo để mô phỏng hồ sơ NCKH theo quy trình quản lý của khoa.", null,
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

    private static async Task EnsureExtendedDemoDataAsync(
        ApplicationDbContext db, long researchOfficeAccountId, CancellationToken cancellationToken)
    {
        var catalogs = new[]
        {
            new JournalConference { Name = "Journal of Educational Data", Type = "TAP_CHI", Issn = "2754-1120", Index = "SCOPUS", Classification = "Q2", Notes = "Danh mục công bố quốc tế" },
            new JournalConference { Name = "Tạp chí Khoa học Giáo dục Việt Nam", Type = "TAP_CHI", Issn = "2615-8957", Index = "TRONG_NUOC", Classification = "UY_TIN", Notes = "Danh mục công bố trong nước" },
            new JournalConference { Name = "Hội nghị FAIR", Type = "HOI_NGHI", Index = "TRONG_NUOC", Classification = "QUOC_GIA", Notes = "Danh mục hội nghị trong nước" },
            new JournalConference { Name = "International Conference on ICT", Type = "HOI_NGHI", Index = "SCOPUS", Classification = "QUOC_TE", Notes = "Danh mục hội nghị quốc tế" }
        };
        var existingCatalogs = await db.JournalConferences
            .Select(x => new { x.Name, x.Type }).ToListAsync(cancellationToken);
        db.JournalConferences.AddRange(catalogs.Where(x => !existingCatalogs.Any(
            e => e.Type == x.Type && e.Name.ToLower() == x.Name.ToLower())));

        var productTypes = await EnsureProductTypesAsync(db, cancellationToken);
        var ruleSpecs = new[]
        {
            (Type: "BAI_BAO", Code: "QD_BAI_BAO", Name: "Quy đổi bài báo khoa học", Value: 120m, Unit: "GIO"),
            (Type: "DE_TAI", Code: "QD_DE_TAI", Name: "Quy đổi đề tài nghiên cứu", Value: 200m, Unit: "GIO"),
            (Type: "SACH", Code: "QD_SACH", Name: "Quy đổi sách và giáo trình", Value: 100m, Unit: "GIO"),
            (Type: "CHUNG_NHAN", Code: "QD_CHUNG_NHAN", Name: "Quy đổi chứng nhận khoa học", Value: 50m, Unit: "GIO")
        };
        foreach (var spec in ruleSpecs)
        {
            var rule = await db.ConversionRules.SingleOrDefaultAsync(
                x => x.Code == spec.Code && x.Version == 1, cancellationToken);
            if (rule is null)
            {
                rule = new ConversionRule(productTypes[spec.Type].Id, spec.Code, 1, spec.Name,
                    "Áp dụng cho sản phẩm đã được phê duyệt trong năm học 2026-2027.", spec.Value, spec.Unit, 100,
                    "Tác giả chính hưởng 100%, đồng tác giả hưởng 50%.", new DateOnly(2026, 1, 1),
                    new DateOnly(2027, 12, 31), "Quy định minh họa phục vụ đồ án");
                db.ConversionRules.Add(rule);
                await db.SaveChangesAsync(cancellationToken);
            }

            if (!await db.AuthorConversionCoefficients.AnyAsync(
                    x => x.ConversionRuleId == rule.Id, cancellationToken))
            {
                db.AuthorConversionCoefficients.AddRange(
                    new AuthorConversionCoefficient(rule.Id, "TAC_GIA_CHINH", 1m),
                    new AuthorConversionCoefficient(rule.Id, "DONG_TAC_GIA", 0.5m));
            }

            if (!await db.ConversionCriteria.AnyAsync(
                    x => x.ConversionRuleId == rule.Id, cancellationToken))
            {
                db.ConversionCriteria.Add(new ConversionCriterion(
                    rule.Id, "NAM_CONG_BO", "TU", null, 2026m));
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        var products = await db.Products.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var lecturerAccounts = await db.Lecturers.ToDictionaryAsync(
            x => x.Id, x => x.AccountId, cancellationToken);
        foreach (var product in products.Where(x => x.Status != ProductStatuses.Draft))
        {
            if (!await db.ProductSnapshots.AnyAsync(x => x.ProductId == product.Id, cancellationToken))
            {
                db.ProductSnapshots.Add(new ProductSnapshot(
                    product, lecturerAccounts[product.SubmittedByLecturerId], 1));
            }
        }

        var returnedProduct = products.FirstOrDefault(x => x.Status == ProductStatuses.Returned);
        if (returnedProduct is not null && !await db.ProductFeedbacks.AnyAsync(
                x => x.ProductId == returnedProduct.Id, cancellationToken))
        {
            db.ProductFeedbacks.Add(new ProductFeedback(returnedProduct.Id, researchOfficeAccountId,
                "Vui lòng bổ sung quyết định giao đề tài và cập nhật đầy đủ thông tin minh chứng trước khi nộp lại."));
        }
        await db.SaveChangesAsync(cancellationToken);

        var academicYear = await db.AcademicYears.SingleAsync(
            x => x.Code == "2026-2027", cancellationToken);
        var classificationRules = new[]
        {
            ("Xuất sắc", 120m, 1), ("Tốt", 100m, 2), ("Khá", 80m, 3), ("Đạt", 50m, 4)
        };
        var existingClassificationNames = await db.EmulationClassificationRules
            .Where(x => x.AcademicYearId == academicYear.Id).Select(x => x.Name).ToListAsync(cancellationToken);
        db.EmulationClassificationRules.AddRange(classificationRules
            .Where(x => !existingClassificationNames.Contains(x.Item1))
            .Select(x => new EmulationClassificationRule(academicYear.Id, x.Item1, x.Item2, x.Item3)));
        await db.SaveChangesAsync(cancellationToken);
        var approvedProducts = await (from p in db.Products
                                      join item in db.ResearchPlanItems on p.ResearchPlanItemId equals item.Id
                                      where p.Status == ProductStatuses.Approved
                                      select new { Product = p, item.ProductTypeId }).ToListAsync(cancellationToken);
        foreach (var approved in approvedProducts)
        {
            var authors = new List<(long LecturerId, string Role)>
            {
                (approved.Product.SubmittedByLecturerId, "TAC_GIA_CHINH")
            };
            authors.AddRange(await db.ProductCoAuthors.Where(x => x.ProductId == approved.Product.Id)
                .Select(x => new ValueTuple<long, string>(x.LecturerId, x.AuthorRole))
                .ToListAsync(cancellationToken));

            foreach (var author in authors)
            {
                var lecturer = await db.Lecturers.SingleAsync(x => x.Id == author.LecturerId, cancellationToken);
                var evaluation = await db.Evaluations.SingleOrDefaultAsync(
                    x => x.LecturerId == lecturer.Id && x.AcademicYearId == academicYear.Id && x.Attempt == 1,
                    cancellationToken);
                if (evaluation is null)
                {
                    evaluation = new Evaluation(lecturer.Id, academicYear.Id, 1, researchOfficeAccountId,
                        lecturer.Code, lecturer.FullName, lecturer.DepartmentId, lecturer.AcademicRank, lecturer.Degree);
                    db.Evaluations.Add(evaluation);
                    await db.SaveChangesAsync(cancellationToken);
                }

                if (!await db.EvaluationConversionDetails.AnyAsync(
                        x => x.EvaluationId == evaluation.Id && x.ProductId == approved.Product.Id,
                        cancellationToken))
                {
                    var rule = await db.ConversionRules.SingleAsync(
                        x => x.ProductTypeId == approved.ProductTypeId && x.Code.StartsWith("QD_"), cancellationToken);
                    var coefficient = await db.AuthorConversionCoefficients
                        .Where(x => x.ConversionRuleId == rule.Id && x.AuthorRole == author.Role)
                        .Select(x => x.Coefficient).SingleAsync(cancellationToken);
                    db.EvaluationConversionDetails.Add(new EvaluationConversionDetail(
                        evaluation.Id, lecturer.Id, academicYear.Id, approved.Product.Id,
                        approved.ProductTypeId, rule.Id, rule.ConversionValue, coefficient, rule.Unit,
                        $"Quy định {rule.Code} phiên bản {rule.Version}; {author.Role} hệ số {coefficient:0.##}"));
                }

                if (evaluation.Status != "CHOT")
                    evaluation.Finalize("HOAN_THANH_TOT", "Có sản phẩm khoa học đã duyệt và đạt định mức minh họa.");
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Tạo tập dữ liệu đủ lớn để biểu diễn một hệ thống đang vận hành: 2 khoa,
    /// 6 bộ môn, 32 giảng viên, 3 năm học, 72 kế hoạch và 144 sản phẩm.
    /// Các bản ghi được đánh dấu bằng tên đăng nhập seed.gv* để có thể chạy lặp lại.
    /// </summary>
    private static async Task EnsureLargeSampleDataAsync(
        ApplicationDbContext db,
        IPasswordService passwordService,
        CancellationToken cancellationToken)
    {
        if (await db.Accounts.AnyAsync(x => x.Username == "seed.gv09", cancellationToken))
            return;

        var dataFaculty = await db.Faculties.SingleOrDefaultAsync(x => x.Code == "KDL", cancellationToken);
        if (dataFaculty is null)
        {
            dataFaculty = new Faculty("KDL", "Khoa Khoa học Dữ liệu");
            db.Faculties.Add(dataFaculty);
            await db.SaveChangesAsync(cancellationToken);
        }

        var currentFaculty = await db.Faculties.SingleAsync(x => x.Code == "CNTT", cancellationToken);
        var departmentSpecs = new[]
        {
            (currentFaculty.Id, "MMT", "Bộ môn Mạng máy tính"),
            (currentFaculty.Id, "ATTT", "Bộ môn An toàn thông tin"),
            (dataFaculty.Id, "KPDL", "Bộ môn Khai phá dữ liệu"),
            (dataFaculty.Id, "TTNT", "Bộ môn Trí tuệ nhân tạo"),
            (dataFaculty.Id, "PTDL", "Bộ môn Phân tích dữ liệu")
        };
        foreach (var (facultyId, code, name) in departmentSpecs)
        {
            if (!await db.Departments.AnyAsync(x => x.Code == code, cancellationToken))
                db.Departments.Add(new Department { FacultyId = facultyId, Code = code, Name = name });
        }
        await db.SaveChangesAsync(cancellationToken);

        var years = new[]
        {
            ("2024-2025", new DateOnly(2024, 8, 1), new DateOnly(2025, 7, 31)),
            ("2025-2026", new DateOnly(2025, 8, 1), new DateOnly(2026, 7, 31)),
            ("2026-2027", new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 31))
        };
        foreach (var (code, start, end) in years)
        {
            if (!await db.AcademicYears.AnyAsync(x => x.Code == code, cancellationToken))
                db.AcademicYears.Add(new AcademicYear { Code = code, StartDate = start, EndDate = end });
        }
        await db.SaveChangesAsync(cancellationToken);

        var departments = await db.Departments.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var names = new[]
        {
            "Ngô Hoàng Phúc", "Lương Mỹ Duyên", "Đỗ Quốc Huy", "Phan Bảo Trâm",
            "Hoàng Gia Hân", "Vũ Minh Khoa", "Trịnh Thanh Thảo", "Đinh Quốc Khánh",
            "Mai Thu Phương", "Hồ Nhật Quang", "Nguyễn Bích Ngọc", "Trần Đức Long",
            "Lê Hoài An", "Phạm Văn Hòa", "Đoàn Minh Tâm", "Bùi Thu Hà",
            "Nguyễn Quốc Đạt", "Tạ Ngọc Diệp", "Võ Thanh Bình", "Huỳnh Gia Linh",
            "Lý Minh Châu", "Trương Đức Thịnh", "Cao Phương Nam", "Dương Khả Vy"
        };
        var newAccounts = new List<Account>();
        for (var i = 0; i < names.Length; i++)
        {
            var ordinal = i + 9;
            var account = new Account($"seed.gv{ordinal:D2}", string.Empty, names[i], $"gv{ordinal:D2}@huit.edu.vn");
            account.SetPasswordHash(passwordService.Hash(account, DefaultPassword));
            account.AddRole(new RoleAssignment(Roles.Lecturer, Scopes.Personal));
            newAccounts.Add(account);
        }
        db.Accounts.AddRange(newAccounts);
        await db.SaveChangesAsync(cancellationToken);

        var newLecturers = new List<Lecturer>();
        for (var i = 0; i < newAccounts.Count; i++)
        {
            var ordinal = i + 9;
            var department = departments[i % departments.Count];
            var degree = i % 3 == 0 ? "TIẾN SĨ" : "THẠC SĨ";
            var rank = i % 7 == 0 ? "PGS" : null;
            newLecturers.Add(new Lecturer(
                department.Id, newAccounts[i].Id, $"GV{ordinal:D3}", names[i],
                new DateOnly(1982 + (i % 13), 1 + (i % 12), 1 + (i % 25)),
                i % 2 == 0 ? "NAM" : "NỮ", $"gv{ordinal:D2}@huit.edu.vn", $"0902{ordinal:D6}",
                rank, degree, i % 3 == 0 ? "Giảng viên chính" : "Giảng viên"));
        }
        db.Lecturers.AddRange(newLecturers);
        await db.SaveChangesAsync(cancellationToken);

        var types = await EnsureProductTypesAsync(db, cancellationToken);
        var academicYears = await db.AcademicYears.OrderBy(x => x.StartDate).ToListAsync(cancellationToken);
        var plans = new List<ResearchPlan>();
        foreach (var lecturer in newLecturers)
        foreach (var year in academicYears)
        {
            var plan = new ResearchPlan(lecturer.Id, year.Id, 2, year.StartDate.AddDays(25), year.EndDate.AddDays(-30));
            plan.Submit();
            plans.Add(plan);
        }
        db.ResearchPlans.AddRange(plans);
        await db.SaveChangesAsync(cancellationToken);

        var planItems = new List<ResearchPlanItem>();
        foreach (var plan in plans)
        {
            var lecturer = newLecturers.Single(x => x.Id == plan.LecturerId);
            var year = academicYears.Single(x => x.Id == plan.AcademicYearId);
            planItems.Add(new ResearchPlanItem(plan.Id, types["BAI_BAO"].Id,
                $"Bài báo chuyên ngành của {lecturer.FullName} ({year.Code})", 1, year.EndDate.AddDays(-90)));
            planItems.Add(new ResearchPlanItem(plan.Id, types["DE_TAI"].Id,
                $"Đề tài ứng dụng số của {lecturer.FullName} ({year.Code})", 1, year.EndDate.AddDays(-30)));
        }
        db.ResearchPlanItems.AddRange(planItems);
        await db.SaveChangesAsync(cancellationToken);

        var products = new List<Product>();
        for (var i = 0; i < planItems.Count; i++)
        {
            var item = planItems[i];
            var plan = plans.Single(x => x.Id == item.ResearchPlanId);
            var lecturer = newLecturers.Single(x => x.Id == plan.LecturerId);
            var isArticle = item.ProductTypeId == types["BAI_BAO"].Id;
            var product = isArticle
                ? NewArticle(item, lecturer, $"Nghiên cứu {i + 1}: Giải pháp chuyển đổi số trong giáo dục", "Tạp chí/Kỷ yếu khoa học", i % 2 == 0 ? "SCOPUS" : "TRONG_NUOC", i % 2 == 0 ? "Q3" : "UY_TIN")
                : NewProject(item, lecturer, $"Đề tài {i + 1}: Nền tảng dữ liệu hỗ trợ quản trị", i % 2 == 0 ? "CẤP_TRƯỜNG" : "CẤP_KHOA");
            switch (i % 7)
            {
                case 1: product.Submit(); break;
                case 2: product.Submit(); product.Approve(ProductStatuses.DepartmentHeadApproved); break;
                case 3: product.Submit(); product.Approve(ProductStatuses.DepartmentHeadApproved); product.Approve(ProductStatuses.FacultyDeanApproved); break;
                case 4: product.Submit(); product.Return(); break;
                case 5: product.Submit(); product.Approve(ProductStatuses.DepartmentHeadApproved); product.Approve(ProductStatuses.FacultyDeanApproved); product.Approve(ProductStatuses.ResearchOfficeApproved); break;
                case 6: product.Submit(); product.Approve(ProductStatuses.DepartmentHeadApproved); product.Approve(ProductStatuses.FacultyDeanApproved); product.Approve(ProductStatuses.ResearchOfficeApproved); product.Approve(ProductStatuses.Approved, 120m); break;
            }
            products.Add(product);
        }
        db.Products.AddRange(products);
        await db.SaveChangesAsync(cancellationToken);

        db.ScientificProfiles.AddRange(newLecturers.Select(lecturer =>
            new ScientificProfile(lecturer.Id, "Công nghệ thông tin", "Chuyển đổi số và dữ liệu", "Nghiên cứu ứng dụng", "Hồ sơ NCKH được khởi tạo phục vụ dữ liệu vận hành.", DateTimeOffset.UtcNow)));
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Tạo kết quả chốt cho năm hiện hành để dashboard/báo cáo có quy mô tương xứng.</summary>
    private static async Task EnsureLargeReportEvaluationsAsync(
        ApplicationDbContext db,
        long researchOfficeAccountId,
        CancellationToken cancellationToken)
    {
        var academicYear = await db.AcademicYears.SingleAsync(x => x.Code == "2026-2027", cancellationToken);
        var seedAccountIds = await db.Accounts.Where(x => x.Username.StartsWith("seed.gv"))
            .Select(x => x.Id).ToListAsync(cancellationToken);
        var seedLecturers = await db.Lecturers.Where(x => seedAccountIds.Contains(x.AccountId))
            .ToListAsync(cancellationToken);
        var articleTypeId = await db.ProductTypes.Where(x => x.Code == "BAI_BAO").Select(x => x.Id).SingleAsync(cancellationToken);
        var rule = await db.ConversionRules
            .Where(x => x.ProductTypeId == articleTypeId && x.Code == "QD_BAI_BAO")
            .SingleAsync(cancellationToken);

        foreach (var lecturer in seedLecturers)
        {
            if (await db.Evaluations.AnyAsync(x => x.LecturerId == lecturer.Id && x.AcademicYearId == academicYear.Id && x.Attempt == 1, cancellationToken))
                continue;

            var product = await (from item in db.ResearchPlanItems
                                 join plan in db.ResearchPlans on item.ResearchPlanId equals plan.Id
                                 join candidate in db.Products on item.Id equals candidate.ResearchPlanItemId
                                 where plan.LecturerId == lecturer.Id
                                    && plan.AcademicYearId == academicYear.Id
                                    && item.ProductTypeId == articleTypeId
                                 select candidate).FirstOrDefaultAsync(cancellationToken);
            if (product is null) continue;
            if (product.Status != ProductStatuses.Approved)
                product.Approve(ProductStatuses.Approved, 120m);

            var evaluation = new Evaluation(lecturer.Id, academicYear.Id, 1, researchOfficeAccountId,
                lecturer.Code, lecturer.FullName, lecturer.DepartmentId, lecturer.AcademicRank, lecturer.Degree);
            evaluation.Finalize(lecturer.Degree == "TIẾN SĨ" ? "HOAN_THANH_TOT" : "HOAN_THANH", "Đủ sản phẩm công bố được quy đổi theo quy định hiện hành.");
            db.Evaluations.Add(evaluation);
            await db.SaveChangesAsync(cancellationToken);
            db.EvaluationConversionDetails.Add(new EvaluationConversionDetail(
                evaluation.Id, lecturer.Id, academicYear.Id, product.Id, articleTypeId, rule.Id,
                rule.ConversionValue, 1m, rule.Unit, $"Quy định {rule.Code} phiên bản {rule.Version}; tác giả chính."));
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
