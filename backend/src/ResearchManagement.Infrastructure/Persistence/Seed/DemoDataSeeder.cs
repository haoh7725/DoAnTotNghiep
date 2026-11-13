using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResearchManagement.Application.Auth.Abstractions;
using ResearchManagement.Application.Lecturers.Models;
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
        var existingMarker = await db.Accounts.SingleOrDefaultAsync(
            x => x.Username == MarkerUsername, cancellationToken);
        if (existingMarker is not null)
        {
            await EnsureRealisticOperationalDatasetAsync(db, cancellationToken);
            await NormalizeApprovedProductDatesAsync(db, cancellationToken);
            await EnsureReferenceConversionRulesAsync(db, cancellationToken);
            await EnsureLargeReportEvaluationsAsync(db, existingMarker.Id, cancellationToken);
            await RepairSeedEvaluationClassificationsAsync(db, cancellationToken);
            logger.LogInformation("Dữ liệu mẫu đã tồn tại; giữ nguyên hồ sơ và kết quả người dùng đã chỉnh sửa.");
            return;
        }

        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();

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
                    new DateOnly(1985, 3, 12), "NU", "nguyenminhanh@huit.edu.vn", "0901000001", null, "TIẾN SĨ", "Giảng viên chính"),
                new Lecturer(softwareDepartment.Id, lecturer2.Id, "CNTT002", "ThS. Trần Ngọc Lan",
                    new DateOnly(1990, 8, 21), "NU", "tranngoclan@huit.edu.vn", "0901000002", null, "THẠC SĨ", "Giảng viên"),
                new Lecturer(informationSystemsDepartment.Id, lecturer3.Id, "CNTT003", "TS. Võ Thành Công",
                    new DateOnly(1983, 11, 5), "NAM", "vothanhcong@huit.edu.vn", "0901000003", "PGS", "TIẾN SĨ", "Giảng viên cao cấp"),
                new Lecturer(informationSystemsDepartment.Id, lecturer4.Id, "CNTT004", "ThS. Đặng Hải Yến",
                    new DateOnly(1992, 6, 17), "NU", "danghaiyen@huit.edu.vn", "0901000004", null, "THẠC SĨ", "Giảng viên")
                ,new Lecturer(softwareDepartment.Id, lecturer5.Id, "CNTT005", "TS. Lê Thanh Sơn",
                    new DateOnly(1987, 2, 10), "NAM", "lethanhson@huit.edu.vn", "0901000005", null, "TIẾN SĨ", "Giảng viên chính")
                ,new Lecturer(informationSystemsDepartment.Id, lecturer6.Id, "CNTT006", "ThS. Phạm Thùy Trang",
                    new DateOnly(1991, 9, 8), "NU", "phamthuytrang@huit.edu.vn", "0901000006", null, "THẠC SĨ", "Giảng viên")
                ,new Lecturer(computerScienceDepartment.Id, lecturer7.Id, "CNTT007", "TS. Nguyễn Khánh Linh",
                    new DateOnly(1986, 5, 23), "NU", "nguyenkhanhlinh@huit.edu.vn", "0901000007", null, "TIẾN SĨ", "Giảng viên chính")
                ,new Lecturer(computerScienceDepartment.Id, lecturer8.Id, "CNTT008", "ThS. Bùi Đức Việt",
                    new DateOnly(1993, 12, 4), "NAM", "buiducviet@huit.edu.vn", "0901000008", null, "THẠC SĨ", "Giảng viên")
            };
            db.Lecturers.AddRange(lecturers);
            await db.SaveChangesAsync(cancellationToken);

            var nguyenMinhAnhProfile = new ScientificProfile(
                lecturers[0].Id,
                "Kỹ thuật phần mềm; học máy ứng dụng; phân tích dữ liệu giáo dục",
                "Trí tuệ nhân tạo, khai phá dữ liệu và công nghệ giáo dục",
                "Mô hình dự báo nguy cơ học vụ; phân tích học tập; hệ thống hỗ trợ ra quyết định trong giáo dục",
                "Giảng viên chính, chủ nhiệm và thành viên nhiều đề tài về trí tuệ nhân tạo trong giáo dục; có công bố trong nước và quốc tế.",
                DateTimeOffset.UtcNow);
            ApplyNguyenMinhAnhProfileDetails(nguyenMinhAnhProfile);

            db.ScientificProfiles.AddRange(
                nguyenMinhAnhProfile,
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
                new ResearchPlanItem(plans[1].Id, productTypes["HOI_NGHI_HOI_THAO"].Id, "Báo cáo tại Hội nghị FAIR về kiểm thử phần mềm", 1, new DateOnly(2027, 5, 15)),
                new ResearchPlanItem(plans[2].Id, productTypes["BAI_BAO"].Id, "Bài báo phân tích dữ liệu giáo dục", 1, new DateOnly(2027, 2, 28)),
                new ResearchPlanItem(plans[2].Id, productTypes["SACH"].Id, "Giáo trình kho dữ liệu", 1, new DateOnly(2027, 6, 1)),
                new ResearchPlanItem(plans[3].Id, productTypes["DE_TAI"].Id, "Đề tài chuyển đổi số cấp khoa", 1, new DateOnly(2027, 4, 30)),
                new ResearchPlanItem(plans[4].Id, productTypes["BAI_BAO"].Id, "Nghiên cứu áp dụng DevOps trong phát triển phần mềm", 1, new DateOnly(2027, 5, 15)),
                new ResearchPlanItem(plans[5].Id, productTypes["BAI_BAO"].Id, "Mô hình hỗ trợ quyết định cho tuyển sinh đại học", 1, new DateOnly(2027, 4, 30)),
                new ResearchPlanItem(plans[6].Id, productTypes["BAI_BAO"].Id, "Nhận diện hành vi học tập từ dữ liệu lớp học", 1, new DateOnly(2027, 3, 15)),
                new ResearchPlanItem(plans[7].Id, productTypes["HOI_NGHI_HOI_THAO"].Id, "Kỷ yếu hội nghị quốc tế về bảo mật hệ thống", 1, new DateOnly(2027, 6, 15))
            };
            items[0].UpdateProgress("DA_THUC_HIEN", "Đã công bố và hoàn tất minh chứng", null);
            items[2].UpdateProgress("DANG_THUC_HIEN", "Đã có kết quả thử nghiệm", "Cần bổ sung bộ dữ liệu kiểm thử");
            items[4].UpdateProgress("DA_THUC_HIEN", "Đã công bố", null);
            db.ResearchPlanItems.AddRange(items);
            await db.SaveChangesAsync(cancellationToken);

            db.ProgressHistories.AddRange(
                new ProgressHistory(items[0].Id, lecturer1.Id, "DA_THUC_HIEN", "Đã công bố và hoàn tất minh chứng", null),
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

            // Phân bố đủ các trạng thái để kiểm thử quy trình xét duyệt và dashboard.
            products[0].Submit();
            products[0].Approve(ProductStatuses.DepartmentHeadApproved);
            products[0].Approve(ProductStatuses.FacultyDeanApproved);
            products[0].Approve(ProductStatuses.ResearchOfficeApproved);
            products[0].Approve(ProductStatuses.Approved, 100m);
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
                new ReviewHistory(products[0].Id, lecturer1.Id, ProductStatuses.Draft, ProductStatuses.Submitted, "Nộp bài báo và minh chứng công bố"),
                new ReviewHistory(products[0].Id, head.Id, ProductStatuses.Submitted, ProductStatuses.DepartmentHeadApproved, "Bộ môn xác nhận nội dung và tác giả"),
                new ReviewHistory(products[0].Id, dean.Id, ProductStatuses.DepartmentHeadApproved, ProductStatuses.FacultyDeanApproved, "Khoa đề nghị công nhận kết quả"),
                new ReviewHistory(products[0].Id, researchOffice.Id, ProductStatuses.FacultyDeanApproved, ProductStatuses.ResearchOfficeApproved, "Minh chứng công bố hợp lệ"),
                new ReviewHistory(products[0].Id, researchOffice.Id, ProductStatuses.ResearchOfficeApproved, ProductStatuses.Approved, "Công nhận sản phẩm và quy đổi"),
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
                    "seed:overdue:item3", items[2].Id, researchPlanId: plans[1].Id, actionPath: "research-plans")),
                new Notification(lecturer1.Id, "NHAC_HAN", "Bài báo dự báo kết quả học tập sắp đến hạn.",
                    "seed:due:item1", items[0].Id, researchPlanId: plans[0].Id, actionPath: "research-plans"),
                new Notification(lecturer4.Id, "PHAN_HOI", "Sản phẩm đã được trả lại: bổ sung quyết định giao đề tài.",
                    "seed:return:product5", researchPlanItemId: items[6].Id, researchPlanId: plans[3].Id,
                    scientificProductId: products[4].Id, actionPath: $"products:{products[4].Id}"));

            await db.SaveChangesAsync(cancellationToken);
            await EnsureExtendedDemoDataAsync(db, researchOffice.Id, cancellationToken);
            await EnsureLargeSampleDataAsync(db, passwordService, cancellationToken);
            await EnsureCompleteProductDemoDataAsync(db, cancellationToken);
            await EnsureReferenceConversionRulesAsync(db, cancellationToken);
            await EnsureUnitManagersAsync(db, passwordService, cancellationToken);
            await EnsureLargeReportEvaluationsAsync(db, researchOffice.Id, cancellationToken);
            await EnsureOperationalDemoDataAsync(db, researchOffice.Id, cancellationToken);
            await RepairSeedEvaluationClassificationsAsync(db, cancellationToken);
            await NormalizeNguyenMinhAnhProductsAsync(db, cancellationToken);
            await EnsureUncommittedDemoLecturerAsync(db, passwordService, cancellationToken);
            await EnsureUncommittedNguyenAnhThuAsync(db, passwordService, cancellationToken);
            await NormalizeApprovedProductDatesAsync(db, cancellationToken);
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

    private static async Task NormalizeApprovedProductDatesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var rows = await (from product in db.Products
                          join item in db.ResearchPlanItems on product.ResearchPlanItemId equals item.Id
                          join plan in db.ResearchPlans on item.ResearchPlanId equals plan.Id
                          join year in db.AcademicYears on plan.AcademicYearId equals year.Id
                          where product.Status == ProductStatuses.Approved && !product.PublishedDate.HasValue
                          select new { Product = product, YearEnd = year.EndDate }).ToListAsync(ct);
        foreach (var row in rows)
        {
            try { row.Product.CorrectMissingRecognitionDate(row.YearEnd.AddDays(-30)); }
            catch (InvalidOperationException) { }
        }
        if (rows.Count > 0) await db.SaveChangesAsync(ct);
    }

    private static async Task RepairSeedEvaluationClassificationsAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var evaluations = await db.Evaluations
            .Where(x => x.Status == "CHOT" && (x.Classification == "HOAN_THANH" || x.Classification == "HOAN_THANH_TOT"))
            .ToListAsync(ct);
        foreach (var evaluation in evaluations)
        {
            var norm = await db.ResearchNorms.AsNoTracking()
                .Where(x => x.LecturerId == evaluation.LecturerId && x.AcademicYearId == evaluation.AcademicYearId)
                .Select(x => new { x.RequiredHours, x.RequiredPoints }).SingleOrDefaultAsync(ct);
            if (norm is null) continue;
            var totals = await db.EvaluationConversionDetails.AsNoTracking()
                .Where(x => x.EvaluationId == evaluation.Id)
                .GroupBy(_ => 1)
                .Select(g => new { Hours = g.Sum(x => x.Unit == "GIO" ? x.ConvertedValue : 0), Points = g.Sum(x => x.Unit == "DIEM" ? x.ConvertedValue : 0) })
                .SingleOrDefaultAsync(ct);
            if (totals is null) continue;
            var unit = evaluation.EvaluationUnit == "DIEM" ? "DIEM" : "GIO";
            var required = unit == "DIEM" ? norm.RequiredPoints : norm.RequiredHours;
            if (required <= 0) continue;
            var value = unit == "DIEM" ? totals.Points : totals.Hours;
            var percent = decimal.Round(value * 100m / required, 2);
            var rule = await db.EmulationClassificationRules.AsNoTracking()
                .Where(x => x.AcademicYearId == evaluation.AcademicYearId && x.MinimumPercent <= percent)
                .OrderByDescending(x => x.MinimumPercent).ThenBy(x => x.SortOrder).FirstOrDefaultAsync(ct);
            var name = rule?.Name ?? "Chưa đạt";
            var basis = $"Dữ liệu mẫu đã chuẩn hóa theo {(unit == "GIO" ? "giờ NCKH" : "điểm NCKH")}: {value:0.####}/{required:0.####} ({percent:0.##}%).";
            evaluation.RecalculateClassification(name, basis);
        }
        if (evaluations.Count > 0) await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureReferenceConversionRulesAsync(ApplicationDbContext db, CancellationToken ct)
    {
        var typeIds = await db.ProductTypes.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id, ct);
        var classIds = await db.ProductClassifications.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id, ct);
        var specs = new[]
        {
            ("BAI_BAO", "SCI", "REF_BAI_BAO_SCI_GIO", "Bài báo thuộc SCI", 1760m),
            ("BAI_BAO", "SCI_E_SSCI_AHCI_ESCI", "REF_BAI_BAO_ISI_GIO", "Bài báo thuộc SCI-E, SSCI, A&HCI hoặc ESCI", 1174m),
            ("BAI_BAO", "SCOPUS", "REF_BAI_BAO_SCOPUS_GIO", "Bài báo thuộc Scopus", 881m),
            ("BAI_BAO", "ISSN_HOIGS", "REF_BAI_BAO_HOIGS_GIO", "Tạp chí quốc gia ISSN thuộc HĐGS", 822m),
            ("BAI_BAO", "ISSN_HOIGS_1_00", "REF_BAI_BAO_HOIGS_1_GIO", "Tạp chí ISSN HĐGS, điểm tối đa 1,0", 763m),
            ("BAI_BAO", "ISSN_HOIGS_0_75", "REF_BAI_BAO_HOIGS_075_GIO", "Tạp chí ISSN HĐGS, điểm tối đa 0,75", 704m),
            ("BAI_BAO", "ISSN_HOIGS_0_50", "REF_BAI_BAO_HOIGS_05_GIO", "Tạp chí ISSN HĐGS, điểm tối đa 0,5", 646m),
            ("BAI_BAO", "ISSN_HOIGS_0_25", "REF_BAI_BAO_HOIGS_025_GIO", "Tạp chí ISSN HĐGS, điểm tối đa 0,25", 587m),
            ("BAI_BAO", "ISSN_PHAN_BIEN", "REF_BAI_BAO_ISSN_PHAN_BIEN_GIO", "Tạp chí có ISSN và phản biện khoa học", 470m),
            ("BAI_BAO", "TAP_CHI_NUOC_NGOAI_PHAN_BIEN", "REF_BAI_BAO_NUOC_NGOAI_GIO", "Tạp chí nước ngoài có phản biện khoa học", 294m),
            ("BAI_BAO", "TAP_CHI_TRONG_NUOC_TOAN_VAN_PHAN_BIEN", "REF_BAI_BAO_TRONG_NUOC_GIO", "Tạp chí trong nước toàn văn có phản biện", 176m),
            ("DE_TAI", "CAP_NHA_NUOC", "REF_DE_TAI_NHA_NUOC_GIO", "Đề tài cấp Nhà nước và tương đương", 1760m),
            ("DE_TAI", "CAP_BO_TINH", "REF_DE_TAI_BO_TINH_GIO", "Đề tài cấp Bộ, tỉnh, thành phố và tương đương", 1174m),
            ("DE_TAI", "CAP_TRUONG_AP_DUNG", "REF_DE_TAI_TRUONG_AP_DUNG_GIO", "Đề tài cấp trường có áp dụng thực tế", 822m),
            ("DE_TAI", "CAP_TRUONG_KHONG_AP_DUNG", "REF_DE_TAI_TRUONG_GIO", "Đề tài cấp trường không có địa chỉ áp dụng", 587m),
            ("HOI_NGHI_HOI_THAO", "KY_YEU_QUOC_TE_ISI", "REF_HOI_NGHI_ISI_GIO", "Kỷ yếu quốc tế thuộc ISI", 1174m),
            ("HOI_NGHI_HOI_THAO", "KY_YEU_QUOC_TE_SCOPUS", "REF_HOI_NGHI_SCOPUS_GIO", "Kỷ yếu quốc tế thuộc Scopus", 822m),
            ("HOI_NGHI_HOI_THAO", "KY_YEU_QUOC_GIA_HOIGS", "REF_HOI_NGHI_HOIGS_GIO", "Kỷ yếu quốc gia thuộc HĐGS", 763m),
            ("HOI_NGHI_HOI_THAO", "KY_YEU_QUOC_TE_PHAN_BIEN_NXB", "REF_HOI_NGHI_QT_PHAN_BIEN_GIO", "Kỷ yếu quốc tế phản biện, NXB uy tín", 704m),
            ("HOI_NGHI_HOI_THAO", "KY_YEU_TRONG_NUOC_PHAN_BIEN_NXB", "REF_HOI_NGHI_TN_PHAN_BIEN_GIO", "Kỷ yếu trong nước phản biện, NXB uy tín", 587m),
            ("HOI_NGHI_HOI_THAO", "BAO_CAO_QUOC_TE_PHAN_BIEN", "REF_BAO_CAO_QT_GIO", "Báo cáo quốc tế có phản biện", 176m),
            ("HOI_NGHI_HOI_THAO", "BAO_CAO_TRONG_NUOC_PHAN_BIEN", "REF_BAO_CAO_TN_GIO", "Báo cáo trong nước có phản biện", 117m),
            ("SACH", "SACH_CHUYEN_KHAO", "REF_SACH_CHUYEN_KHAO_GIO", "Sách chuyên khảo", 1760m),
            ("SACH", "SACH_THAM_KHAO", "REF_SACH_THAM_KHAO_GIO", "Sách tham khảo", 1174m),
            ("SACH", "GIAO_TRINH_CAP_TRUONG", "REF_GIAO_TRINH_TRUONG_GIO", "Giáo trình nghiệm thu cấp trường", 587m),
            ("SACH", "GIAO_TRINH_NOI_BO_BIEN_SOAN_MOI", "REF_GIAO_TRINH_NOI_BO_MOI_GIO", "Giáo trình nội bộ biên soạn mới", 470m),
            ("SACH", "GIAO_TRINH_NOI_BO_SUA_30", "REF_GIAO_TRINH_NOI_BO_SUA_GIO", "Giáo trình nội bộ chỉnh sửa tối thiểu 30%", 176m),
            ("SACH", "GIAO_TRINH_THUC_HANH_BIEN_SOAN_MOI", "REF_GIAO_TRINH_THUC_HANH_MOI_GIO", "Giáo trình thực hành biên soạn mới", 352m),
            ("SACH", "GIAO_TRINH_THUC_HANH_SUA_30", "REF_GIAO_TRINH_THUC_HANH_SUA_GIO", "Giáo trình thực hành chỉnh sửa tối thiểu 30%", 117m)
        };
        foreach (var spec in specs)
        {
            if (!typeIds.TryGetValue(spec.Item1, out var typeId) || !classIds.TryGetValue(spec.Item2, out var classId)) continue;
            var rule = await db.ConversionRules.SingleOrDefaultAsync(x => x.Code == spec.Item3 && x.Version == 1, ct);
            if (rule is null)
            {
                rule = new ConversionRule(typeId, spec.Item3, 1, spec.Item4,
                    "Mức tham khảo theo bảng quy đổi trong tài liệu quydinhquydoi.docx; cần QLKH xác nhận trước khi áp dụng.",
                    spec.Item5, "GIO", 200, "Tác giả chính và đồng tác giả theo quy định được cấu hình.",
                    new DateOnly(2024, 1, 1), new DateOnly(2027, 12, 31), "Tài liệu tham khảo quy đổi NCKH");
                rule.ClassificationId = classId;
                db.ConversionRules.Add(rule);
                await db.SaveChangesAsync(ct);
            }
            if (!await db.AuthorConversionCoefficients.AnyAsync(x => x.ConversionRuleId == rule.Id, ct))
                db.AuthorConversionCoefficients.AddRange(new AuthorConversionCoefficient(rule.Id, "TAC_GIA_CHINH", 1m), new AuthorConversionCoefficient(rule.Id, "DONG_TAC_GIA", 0.5m));
            if (!await db.ConversionCriteria.AnyAsync(x => x.ConversionRuleId == rule.Id, ct))
                db.ConversionCriteria.Add(new ConversionCriterion(rule.Id, "NAM_CONG_BO", "TU", null, 2024m));
        }
        await db.SaveChangesAsync(ct);
    }

    private static Product NewArticle(ResearchPlanItem item, Lecturer lecturer, string title,
        string publication, string? index, string? classification) =>
        new(item.Id, lecturer.Id, title, "Sản phẩm được khởi tạo để mô phỏng hồ sơ NCKH theo quy trình quản lý của khoa.", publication,
            new DateOnly(2026, 10, 15), null, index, classification);

    private static Product NewProject(ResearchPlanItem item, Lecturer lecturer, string title, string level) =>
        new(item.Id, lecturer.Id, title, "Sản phẩm được khởi tạo để mô phỏng hồ sơ NCKH theo quy trình quản lý của khoa.", null,
            null, projectLevel: level);

    /// <summary>
    /// Bổ sung bộ sản phẩm hoàn chỉnh cho các giảng viên seed. Mỗi kế hoạch có đủ bốn nhóm
    /// sản phẩm theo danh mục quy đổi; chỉ thêm nội dung còn thiếu để chạy an toàn trên DB cũ.
    /// </summary>
    private static async Task EnsureCompleteProductDemoDataAsync(
        ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var types = await db.ProductTypes
            .Where(x => x.IsActive)
            .ToDictionaryAsync(x => x.Code, cancellationToken);
        var requiredCodes = new[] { "BAI_BAO", "DE_TAI", "HOI_NGHI_HOI_THAO", "SACH" };
        if (requiredCodes.Any(code => !types.ContainsKey(code))) return;

        var lecturers = await (from lecturer in db.Lecturers
                               join account in db.Accounts on lecturer.AccountId equals account.Id
                               where account.Username.StartsWith("seed.gv") || account.Username.StartsWith("gv.")
                               orderby lecturer.Id
                               select lecturer).ToListAsync(cancellationToken);
        if (lecturers.Count == 0) return;

        var years = await db.AcademicYears.OrderByDescending(x => x.StartDate).Take(3).ToListAsync(cancellationToken);
        var journals = await db.JournalConferences.AsNoTracking().ToListAsync(cancellationToken);
        var classifications = await db.ProductClassifications.AsNoTracking()
            .Where(x => x.IsActive)
            .ToListAsync(cancellationToken);
        var plans = await db.ResearchPlans
            .Where(x => lecturers.Select(l => l.Id).Contains(x.LecturerId))
            .ToListAsync(cancellationToken);
        var items = await db.ResearchPlanItems.ToListAsync(cancellationToken);
        var products = await db.Products.ToListAsync(cancellationToken);
        var productKeys = products.Select(x => x.ResearchPlanItemId).ToHashSet();

        foreach (var lecturer in lecturers)
        foreach (var year in years)
        {
            var plan = plans.FirstOrDefault(x => x.LecturerId == lecturer.Id && x.AcademicYearId == year.Id);
            if (plan is null)
            {
                plan = new ResearchPlan(lecturer.Id, year.Id, requiredCodes.Length,
                    year.StartDate.AddDays(25), year.EndDate.AddDays(-30));
                plan.Submit();
                db.ResearchPlans.Add(plan);
                await db.SaveChangesAsync(cancellationToken);
                plans.Add(plan);
            }
            plan.Update(4, year.EndDate.AddDays(-30));
            if (plan.Status == "NHAP") plan.Submit();

            foreach (var (code, index) in requiredCodes.Select((code, index) => (code, index)))
            {
                var type = types[code];
                var item = items.FirstOrDefault(x => x.ResearchPlanId == plan.Id && x.ProductTypeId == type.Id);
                if (item is null)
                {
                    var name = code switch
                    {
                        "BAI_BAO" => $"Bài báo về dữ liệu và chuyển đổi số của {lecturer.FullName}",
                        "DE_TAI" => $"Đề tài ứng dụng công nghệ số của {lecturer.FullName}",
                        "HOI_NGHI_HOI_THAO" => $"Báo cáo hội thảo về đổi mới công nghệ của {lecturer.FullName}",
                        _ => $"Giáo trình và tài liệu chuyên môn của {lecturer.FullName}"
                    };
                    item = new ResearchPlanItem(plan.Id, type.Id, name, 1, year.EndDate.AddDays(-30));
                    db.ResearchPlanItems.Add(item);
                    await db.SaveChangesAsync(cancellationToken);
                    items.Add(item);
                }
                if (productKeys.Contains(item.Id)) continue;

                var journal = code == "BAI_BAO"
                    ? journals.FirstOrDefault(x => x.Type == "TAP_CHI")
                    : journals.FirstOrDefault(x => x.Type == "HOI_NGHI");
                var product = code switch
                {
                    "BAI_BAO" => NewArticle(item, lecturer,
                        $"{lecturer.FullName}: Mô hình phân tích dữ liệu giáo dục ({year.Code})",
                        journal?.Name ?? "Tạp chí khoa học", "SCOPUS", "Q2"),
                    "DE_TAI" => NewProject(item, lecturer,
                        $"{lecturer.FullName}: Nền tảng hỗ trợ quản trị nghiên cứu ({year.Code})",
                        index % 2 == 0 ? "CẤP_TRƯỜNG" : "CẤP_KHOA"),
                    "HOI_NGHI_HOI_THAO" => new Product(item.Id, lecturer.Id,
                        $"{lecturer.FullName}: Báo cáo kết quả nghiên cứu ({year.Code})",
                        "Báo cáo/kỷ yếu hội nghị, hội thảo theo quy định.", journal?.Name ?? "Hội nghị khoa học",
                        year.EndDate.AddDays(-75), "DA_XUAT_BAN", null, null, null,
                        journalConferenceId: journal?.Id),
                    _ => new Product(item.Id, lecturer.Id,
                        $"{lecturer.FullName}: Giáo trình chuyên ngành ({year.Code})",
                        "Sách, giáo trình hoặc tài liệu phục vụ đào tạo.", "Nhà xuất bản Đại học Công Thương",
                        year.EndDate.AddDays(-60))
                };
                var classification = classifications.FirstOrDefault(x => x.ProductTypeId == type.Id);
                if (classification is not null) product.ClassificationId = classification.Id;
                if (code == "BAI_BAO")
                    product.UpdateArticleProgress("DA_XUAT_BAN", year.EndDate.AddDays(-75), journal?.Name);
                product.Submit();
                product.Approve(ProductStatuses.DepartmentHeadApproved);
                product.Approve(ProductStatuses.FacultyDeanApproved);
                product.Approve(ProductStatuses.ResearchOfficeApproved);
                product.Approve(ProductStatuses.Approved, code == "DE_TAI" ? 822m : 120m);
                item.UpdateProgress("DA_THUC_HIEN", "Đã hoàn thành và được phê duyệt.", null);
                if (!await db.ProgressHistories.AnyAsync(x => x.ResearchPlanItemId == item.Id, cancellationToken))
                    db.ProgressHistories.Add(new ProgressHistory(
                        item.Id, lecturer.AccountId, "DA_THUC_HIEN", "Đã hoàn thành và được phê duyệt.", null));
                db.Products.Add(product);
                products.Add(product);
                productKeys.Add(item.Id);
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        var officeAccountId = await db.Accounts.Where(x => x.Username == "qlkh")
            .Select(x => x.Id).SingleAsync(cancellationToken);
        foreach (var product in products)
        {
            if (!await db.ReviewHistories.AnyAsync(x => x.ProductId == product.Id, cancellationToken))
            {
                var owner = await db.Lecturers.SingleAsync(x => x.Id == product.SubmittedByLecturerId, cancellationToken);
                var departmentHeadId = await db.RoleAssignments
                    .Where(x => x.Role == Roles.DepartmentHead && x.DepartmentId == owner.DepartmentId)
                    .Select(x => x.AccountId).FirstOrDefaultAsync(cancellationToken);
                var facultyId = await db.Departments.Where(x => x.Id == owner.DepartmentId)
                    .Select(x => x.FacultyId).SingleAsync(cancellationToken);
                var deanId = await db.RoleAssignments
                    .Where(x => x.Role == Roles.FacultyDean && x.FacultyId == facultyId)
                    .Select(x => x.AccountId).FirstOrDefaultAsync(cancellationToken);
                db.ReviewHistories.AddRange(
                    new ReviewHistory(product.Id, owner.AccountId, ProductStatuses.Draft, ProductStatuses.Submitted, "Giảng viên nộp sản phẩm."),
                    new ReviewHistory(product.Id, departmentHeadId == 0 ? officeAccountId : departmentHeadId, ProductStatuses.Submitted, ProductStatuses.DepartmentHeadApproved, "Bộ môn xác nhận nội dung."),
                    new ReviewHistory(product.Id, deanId == 0 ? officeAccountId : deanId, ProductStatuses.DepartmentHeadApproved, ProductStatuses.FacultyDeanApproved, "Khoa thống nhất đề nghị công nhận."),
                    new ReviewHistory(product.Id, officeAccountId, ProductStatuses.FacultyDeanApproved, ProductStatuses.ResearchOfficeApproved, "Phòng QLKH kiểm tra minh chứng."),
                    new ReviewHistory(product.Id, officeAccountId, ProductStatuses.ResearchOfficeApproved, ProductStatuses.Approved, "Công nhận sản phẩm."));
            }

            if (!await db.ProductSnapshots.AnyAsync(x => x.ProductId == product.Id, cancellationToken))
            {
                var ownerAccountId = await db.Lecturers.Where(x => x.Id == product.SubmittedByLecturerId)
                    .Select(x => x.AccountId).SingleAsync(cancellationToken);
                db.ProductSnapshots.Add(new ProductSnapshot(product, ownerAccountId, 1));
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        var productIds = products.Where(x => x.Id > 0).Select(x => x.Id).ToHashSet();
        var existingCoAuthors = await db.ProductCoAuthors
            .Where(x => productIds.Contains(x.ProductId)).ToListAsync(cancellationToken);
        foreach (var product in products.Where(x => productIds.Contains(x.Id)))
        {
            if (existingCoAuthors.Any(x => x.ProductId == product.Id)) continue;
            var ownerIndex = lecturers.FindIndex(x => x.Id == product.SubmittedByLecturerId);
            if (ownerIndex < 0 || lecturers.Count < 2) continue;
            var coAuthor = lecturers[(ownerIndex + 1) % lecturers.Count];
            db.ProductCoAuthors.Add(new ProductCoAuthor(product.Id, coAuthor.Id, 2));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Thay bộ dữ liệu vận hành cũ bằng một bộ dữ liệu nghiệp vụ nhất quán.
    /// Các bảng nền (tài khoản, hồ sơ, danh mục công bố, quy định quy đổi,
    /// quy chế xếp loại và loại sản phẩm) không nằm trong danh sách xóa.
    /// Chạy đúng một lần trên database hiện có nhờ marker trong ung_dung_migration.
    /// </summary>
    private static async Task EnsureRealisticOperationalDatasetAsync(
        ApplicationDbContext db,
        CancellationToken ct)
    {
        var alreadyRebuilt = await db.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*)::int AS \"Value\" FROM nckh.ung_dung_migration WHERE ma = 'realistic_operational_dataset_v2'")
            .SingleAsync(ct) > 0;
        if (alreadyRebuilt) return;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE
                nckh.bao_cao_xuat,
                nckh.chi_tiet_quy_doi,
                nckh.chi_tiet_quy_doi_ung_dung,
                nckh.dinh_muc_nckh,
                nckh.dong_tac_gia,
                nckh.ke_hoach_nckh,
                nckh.ket_qua_danh_gia,
                nckh.kiem_tra_tuong_dong,
                nckh.lich_su_gia_han_chi_tieu,
                nckh.lich_su_ke_hoach,
                nckh.lich_su_tien_do,
                nckh.lich_su_xet_duyet,
                nckh.lich_su_xet_duyet_san_pham,
                nckh.minh_chung,
                nckh.minh_chung_san_pham,
                nckh.noi_dung_ke_hoach,
                nckh.noi_dung_ke_hoach_san_pham,
                nckh.phan_bo_chi_tieu,
                nckh.phan_cong_chi_tieu_giang_vien,
                nckh.phan_hoi_san_pham,
                nckh.phan_hoi_san_pham_ung_dung,
                nckh.san_pham,
                nckh.san_pham_khoa_hoc,
                nckh.san_pham_phien_ban,
                nckh.san_pham_phien_ban_ung_dung,
                nckh.tham_gia_san_pham,
                nckh.thong_bao,
                nckh.chi_tieu_nckh
            RESTART IDENTITY CASCADE;
            """, ct);

        var activeTypes = await db.ProductTypes
            .Where(x => x.Code != "CHUNG_NHAN")
            .ToDictionaryAsync(x => x.Code, ct);
        if (activeTypes.Count < 4)
            throw new InvalidOperationException("Danh mục loại sản phẩm chưa đủ bốn loại NCKH cần seed.");
        var certificateType = await db.ProductTypes.SingleOrDefaultAsync(x => x.Code == "CHUNG_NHAN", ct);
        if (certificateType is not null) certificateType.IsActive = false;

        var lecturers = await db.Lecturers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);
        var years = await db.AcademicYears.OrderByDescending(x => x.StartDate).Take(3).ToListAsync(ct);
        if (lecturers.Count == 0 || years.Count == 0)
            throw new InvalidOperationException("Cần có giảng viên và năm học nền trước khi seed dữ liệu vận hành.");

        foreach (var lecturer in lecturers)
        foreach (var year in years)
        {
            var hours = lecturer.AcademicRank?.Contains("TIẾN SĨ", StringComparison.OrdinalIgnoreCase) == true ? 240m : 200m;
            db.ResearchNorms.Add(new ResearchNorm(lecturer.Id, year.Id, hours,
                "Quy chế định mức NCKH theo năm học", hours / 20m));
        }
        await db.SaveChangesAsync(ct);

        // Tạo kế hoạch, bốn loại nội dung và sản phẩm qua cùng một luồng dữ liệu.
        await EnsureCompleteProductDemoDataAsync(db, ct);

        // Giữ đủ trạng thái để kiểm thử: đã công nhận, chờ bộ môn, chờ bổ sung
        // và hồ sơ đang gửi. Người dùng vẫn có thể tiếp tục hoàn tất từng luồng.
        var seededProducts = await (from product in db.Products
                                    join item in db.ResearchPlanItems on product.ResearchPlanItemId equals item.Id
                                    join type in db.ProductTypes on item.ProductTypeId equals type.Id
                                    orderby product.Id
                                    select new { Product = product, TypeCode = type.Code })
            .ToListAsync(ct);
        foreach (var (row, index) in seededProducts.Select((row, index) => (row, index)))
        {
            if (index % 12 == 0)
            {
                row.Product.Return();
                if (row.TypeCode == "BAI_BAO")
                    row.Product.UpdateArticleProgress("DANG_VIET", null, row.Product.PublicationInfo);
            }
            else if (index % 12 == 1)
            {
                row.Product.Return();
                if (row.TypeCode == "BAI_BAO")
                    row.Product.UpdateArticleProgress("DANG_PHAN_BIEN", null, row.Product.PublicationInfo);
                row.Product.Submit();
                row.Product.Approve(ProductStatuses.DepartmentHeadApproved);
            }
            else if (index % 12 == 2)
            {
                row.Product.Return();
                if (row.TypeCode == "BAI_BAO")
                    row.Product.UpdateArticleProgress("DANG_VIET", null, row.Product.PublicationInfo);
            }
        }
        await db.SaveChangesAsync(ct);

        var qlkhId = await db.Accounts.Where(x => x.Username == MarkerUsername).Select(x => x.Id).SingleAsync(ct);
        var deanId = await (from role in db.RoleAssignments
                            join account in db.Accounts on role.AccountId equals account.Id
                            where role.Role == Roles.FacultyDean
                            select account.Id).FirstOrDefaultAsync(ct);
        if (deanId == 0) deanId = qlkhId;
        var currentYear = years[0];
        var departments = await db.Departments.AsNoTracking().ToListAsync(ct);
        var facultyIds = lecturers.Join(departments, l => l.DepartmentId, d => d.Id, (_, d) => d.FacultyId)
            .Distinct().ToList();

        foreach (var facultyId in facultyIds)
        foreach (var typeCode in new[] { "BAI_BAO", "DE_TAI", "SACH", "HOI_NGHI_HOI_THAO" })
        {
            var type = activeTypes[typeCode];
            var facultyLecturers = lecturers.Where(l => departments.Any(d => d.Id == l.DepartmentId && d.FacultyId == facultyId)).ToList();
            if (facultyLecturers.Count == 0) continue;

            var target = new ResearchTarget(facultyId, currentYear.Id, facultyLecturers.Count,
                currentYear.EndDate.AddDays(-30), productTypeId: type.Id);
            target.MarkAssigned(qlkhId);
            target.Accept(deanId, DateTimeOffset.UtcNow);
            target.Publish(DateTimeOffset.UtcNow);
            target.Lock(DateTimeOffset.UtcNow);
            db.ResearchTargets.Add(target);
            await db.SaveChangesAsync(ct);

            var allocations = new List<ResearchTargetAllocation>();
            foreach (var department in departments.Where(d => d.FacultyId == facultyId))
            {
                var departmentLecturers = facultyLecturers.Where(l => l.DepartmentId == department.Id).ToList();
                if (departmentLecturers.Count == 0) continue;
                var allocation = new ResearchTargetAllocation(target.Id, facultyId, department.Id,
                    departmentLecturers.Count, currentYear.StartDate.AddDays(30));
                allocations.Add(allocation);
                db.ResearchTargetAllocations.Add(allocation);
            }
            await db.SaveChangesAsync(ct);
            foreach (var allocation in allocations)
            foreach (var lecturer in facultyLecturers.Where(l => l.DepartmentId == allocation.DepartmentId))
                db.LecturerTargetAllocations.Add(new LecturerTargetAllocation(
                    allocation.Id, lecturer.Id, 1, currentYear.StartDate.AddDays(31)));
            await db.SaveChangesAsync(ct);

            var products = await (from product in db.Products
                                  join item in db.ResearchPlanItems on product.ResearchPlanItemId equals item.Id
                                  join plan in db.ResearchPlans on item.ResearchPlanId equals plan.Id
                                  where plan.AcademicYearId == currentYear.Id && item.ProductTypeId == type.Id
                                      && facultyLecturers.Select(x => x.Id).Contains(product.SubmittedByLecturerId)
                                      && product.ResearchTargetId == null
                                  select product).ToListAsync(ct);
            foreach (var product in products.GroupBy(x => x.SubmittedByLecturerId).Select(x => x.First()))
                product.LinkTarget(target.Id, product.JournalConferenceId);
            await db.SaveChangesAsync(ct);
        }

        await db.Database.ExecuteSqlRawAsync(
            "INSERT INTO nckh.ung_dung_migration(ma) VALUES ('realistic_operational_dataset_v2') ON CONFLICT (ma) DO NOTHING", ct);
        await transaction.CommitAsync(ct);
    }

    /// <summary>Chuẩn hóa bộ dữ liệu trình diễn của Nguyễn Minh Anh sau mọi seed bổ sung.</summary>
    private static async Task NormalizeNguyenMinhAnhProductsAsync(
        ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var lecturer = await (from item in db.Lecturers
                              join account in db.Accounts on item.AccountId equals account.Id
                              where account.Username == "gv.nguyenminhanh"
                              select item).SingleOrDefaultAsync(cancellationToken);
        if (lecturer is null) return;
        if (lecturer.Code is "CNTT001" or "CNTT002" or "CNTT003" or "CNTT004" or "CNTT005")
            return;

        var year = await db.AcademicYears.SingleOrDefaultAsync(x => x.Code == "2026-2027", cancellationToken);
        if (year is null) return;
        var plan = await db.ResearchPlans.SingleOrDefaultAsync(
            x => x.LecturerId == lecturer.Id && x.AcademicYearId == year.Id, cancellationToken);
        if (plan is null) return;
        // Kế hoạch demo thống nhất với bốn nội dung đang hiển thị:
        // một bài báo, một đề tài, một hội nghị và một sách.
        plan.Update(4, year.EndDate.AddDays(-30));
        if (plan.Status == "NHAP") plan.Submit();

        var types = await db.ProductTypes.ToDictionaryAsync(x => x.Code, cancellationToken);
        var typeCodes = new[] { "BAI_BAO", "DE_TAI", "HOI_NGHI_HOI_THAO", "SACH" };
        var typeIds = typeCodes.Where(types.ContainsKey).Select(code => types[code].Id).ToHashSet();
        var items = await db.ResearchPlanItems
            .Where(x => x.ResearchPlanId == plan.Id && typeIds.Contains(x.ProductTypeId))
            .ToListAsync(cancellationToken);
        var products = await db.Products
            .Where(x => items.Select(i => i.Id).Contains(x.ResearchPlanItemId))
            .ToListAsync(cancellationToken);
        var catalogs = await db.JournalConferences.AsNoTracking().ToListAsync(cancellationToken);
        var classifications = await db.ProductClassifications.AsNoTracking().ToListAsync(cancellationToken);
        var coAuthors = await db.ProductCoAuthors
            .Where(x => products.Select(p => p.Id).Contains(x.ProductId)).ToListAsync(cancellationToken);
        db.ProductCoAuthors.RemoveRange(coAuthors);

        var otherLecturers = await db.Lecturers.Where(x => x.Id != lecturer.Id)
            .Where(x => x.Code != "CNTT001" && x.Code != "CNTT002" && x.Code != "CNTT003" && x.Code != "CNTT004" && x.Code != "CNTT005")
            .OrderBy(x => x.Id).Take(4).ToListAsync(cancellationToken);
        foreach (var (code, index) in typeCodes.Select((value, index) => (value, index)))
        {
            if (!types.TryGetValue(code, out var type)) continue;
            var item = items.FirstOrDefault(x => x.ProductTypeId == type.Id);
            if (item is null) continue;
            var product = products.FirstOrDefault(x => x.ResearchPlanItemId == item.Id);
            if (product is null) continue;
            // Bộ dữ liệu hồ sơ demo đại diện cho các kết quả đã hoàn thành và được công nhận.
            // Chuẩn hóa trạng thái trước khi dựng ngày xuất bản/tiến độ để tránh lệch dữ liệu cũ.
            product.Submit();
            product.Approve(ProductStatuses.DepartmentHeadApproved);
            product.Approve(ProductStatuses.FacultyDeanApproved);
            product.Approve(ProductStatuses.ResearchOfficeApproved);
            product.Approve(ProductStatuses.Approved, code == "DE_TAI" ? 822m : 120m);
            var classificationCode = code switch
            {
                "BAI_BAO" => "ISSN_HOIGS",
                "DE_TAI" => "CAP_TRUONG_AP_DUNG",
                "HOI_NGHI_HOI_THAO" => "KY_YEU_QUOC_GIA_HOIGS",
                _ => "GIAO_TRINH_CAP_TRUONG"
            };
            var classification = classifications.FirstOrDefault(x =>
                x.ProductTypeId == type.Id && x.Code == classificationCode);
            var journal = code == "BAI_BAO"
                ? catalogs.FirstOrDefault(x => x.Type == "TAP_CHI" && x.Name.Contains("Khoa học Giáo dục"))
                : catalogs.FirstOrDefault(x => x.Type == "HOI_NGHI" && x.Name == "Hội nghị FAIR");
            var title = code switch
            {
                "BAI_BAO" => "Mô hình dự báo nguy cơ học vụ bằng học máy",
                "DE_TAI" => "Trợ lý học tập thông minh cho sinh viên",
                "HOI_NGHI_HOI_THAO" => "Báo cáo kết quả nghiên cứu tại Hội nghị FAIR",
                _ => "Giáo trình phân tích dữ liệu giáo dục đại học"
            };
            var publication = code switch
            {
                "BAI_BAO" => journal?.Name ?? "Tạp chí Khoa học Giáo dục Việt Nam",
                "HOI_NGHI_HOI_THAO" => journal?.Name ?? "Hội nghị FAIR",
                "SACH" => "Nhà xuất bản Đại học Công Thương",
                _ => null
            };
            DateOnly? publishedDate = code switch
            {
                "BAI_BAO" => new DateOnly(2026, 10, 15),
                "HOI_NGHI_HOI_THAO" => new DateOnly(2026, 10, 20),
                "SACH" => new DateOnly(2026, 10, 25),
                _ => null
            };
            product.Update(title, $"Sản phẩm NCKH của {lecturer.FullName}, đồng bộ với kế hoạch năm học {year.Code}.",
                publication, publishedDate,
                code == "BAI_BAO" ? "DA_XUAT_BAN" : null,
                code == "BAI_BAO" ? "TRONG_NUOC" : null,
                code == "BAI_BAO" ? "UY_TIN" : null,
                code == "DE_TAI" ? "CẤP_TRƯỜNG" : null);
            product.SetJournalConference(journal?.Id);
            product.ClassificationId = classification?.Id;
            item.UpdateProgress("DA_THUC_HIEN", "Đã hoàn thành và được phê duyệt.", null);
            if (otherLecturers.Count > index)
                db.ProductCoAuthors.Add(new ProductCoAuthor(product.Id, otherLecturers[index].Id, 2));
        }

        var articleType = types["BAI_BAO"];
        var articleItem = items.First(x => x.ProductTypeId == articleType.Id);
        articleItem.Update(articleItem.Name, 1, articleItem.Deadline);

        var facultyIdForTarget = await db.Departments.Where(x => x.Id == lecturer.DepartmentId)
            .Select(x => x.FacultyId).SingleAsync(cancellationToken);
        var target = await db.ResearchTargets
            .Where(x => x.FacultyId == facultyIdForTarget && x.AcademicYearId == year.Id)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var articleJournalId = catalogs.FirstOrDefault(x =>
            x.Type == "TAP_CHI" && x.Name.Contains("Khoa học Giáo dục"))?.Id;
        if (target is not null)
        {
            foreach (var article in products.Where(x => x.ResearchPlanItemId == articleItem.Id && !x.ResearchTargetId.HasValue))
                article.LinkTarget(target.Id, articleJournalId);
        }

        var officeAccountId = await db.Accounts.Where(x => x.Username == "qlkh")
            .Select(x => x.Id).SingleAsync(cancellationToken);
        var departmentHeadAccountId = await db.RoleAssignments
            .Where(x => x.Role == Roles.DepartmentHead && x.DepartmentId == lecturer.DepartmentId)
            .Select(x => x.AccountId).FirstAsync(cancellationToken);
        var facultyId = await db.Departments.Where(x => x.Id == lecturer.DepartmentId)
            .Select(x => x.FacultyId).SingleAsync(cancellationToken);
        var facultyDeanAccountId = await db.RoleAssignments
            .Where(x => x.Role == Roles.FacultyDean && x.FacultyId == facultyId)
            .Select(x => x.AccountId).FirstAsync(cancellationToken);

        foreach (var product in products)
        {
            var history = await db.ReviewHistories.Where(x => x.ProductId == product.Id)
                .OrderBy(x => x.OccurredAt).ToListAsync(cancellationToken);
            var transitions = new[]
            {
                (ProductStatuses.Draft, ProductStatuses.Submitted, lecturer.AccountId, "Giảng viên nộp sản phẩm."),
                (ProductStatuses.Submitted, ProductStatuses.DepartmentHeadApproved, departmentHeadAccountId, "Bộ môn xác nhận nội dung."),
                (ProductStatuses.DepartmentHeadApproved, ProductStatuses.FacultyDeanApproved, facultyDeanAccountId, "Khoa thống nhất đề nghị công nhận."),
                (ProductStatuses.FacultyDeanApproved, ProductStatuses.ResearchOfficeApproved, officeAccountId, "Phòng QLKH kiểm tra minh chứng."),
                (ProductStatuses.ResearchOfficeApproved, ProductStatuses.Approved, officeAccountId, "Công nhận sản phẩm.")
            };
            foreach (var transition in transitions)
            {
                if (history.Any(x => x.FromStatus == transition.Item1 && x.ToStatus == transition.Item2))
                    continue;
                db.ReviewHistories.Add(new ReviewHistory(product.Id, transition.Item3,
                    transition.Item1, transition.Item2, transition.Item4));
            }

            var latestSnapshot = await db.ProductSnapshots.Where(x => x.ProductId == product.Id)
                .OrderByDescending(x => x.Version).FirstOrDefaultAsync(cancellationToken);
            var hasCurrentSnapshot = latestSnapshot is not null &&
                latestSnapshot.Title == product.Title && latestSnapshot.Status == product.Status &&
                latestSnapshot.Description == product.Description && latestSnapshot.PublicationInfo == product.PublicationInfo &&
                latestSnapshot.PublishedDate == product.PublishedDate && latestSnapshot.ArticleStatus == product.ArticleStatus &&
                latestSnapshot.JournalIndex == product.JournalIndex &&
                latestSnapshot.JournalClassification == product.JournalClassification &&
                latestSnapshot.ProjectLevel == product.ProjectLevel;
            if (!hasCurrentSnapshot)
                db.ProductSnapshots.Add(new ProductSnapshot(product, lecturer.AccountId,
                    (latestSnapshot?.Version ?? 0) + 1));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

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
            ["CHUNG_NHAN"] = "Chứng nhận",
            ["HOI_NGHI_HOI_THAO"] = "Bài/kỷ yếu/báo cáo hội nghị, hội thảo"
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
            (Type: "BAI_BAO", Code: "QD_DIEM_BAI_BAO", Name: "Quy đổi điểm bài báo khoa học", Value: 20m, Unit: "DIEM"),
            (Type: "DE_TAI", Code: "QD_DE_TAI", Name: "Quy đổi đề tài nghiên cứu", Value: 200m, Unit: "GIO"),
            (Type: "SACH", Code: "QD_SACH", Name: "Quy đổi sách, giáo trình và tài liệu", Value: 100m, Unit: "GIO"),
            (Type: "HOI_NGHI_HOI_THAO", Code: "QD_HOI_NGHI_HOI_THAO", Name: "Quy đổi bài, kỷ yếu và báo cáo hội nghị, hội thảo", Value: 100m, Unit: "GIO")
        };
        foreach (var spec in ruleSpecs)
        {
            var rule = await db.ConversionRules.SingleOrDefaultAsync(
                x => x.Code == spec.Code && x.Version == 1, cancellationToken);
            if (rule is null)
            {
                rule = new ConversionRule(productTypes[spec.Type].Id, spec.Code, 1, spec.Name,
                    "Áp dụng cho sản phẩm đã được phê duyệt từ năm học 2024-2025.", spec.Value, spec.Unit, 100,
                    "Tác giả chính hưởng 100%, đồng tác giả hưởng 50%.", new DateOnly(2024, 1, 1),
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
                    rule.Id, "NAM_CONG_BO", "TU", null, 2024m));
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

                var applicableRules = await db.ConversionRules
                    .Where(x => x.ProductTypeId == approved.ProductTypeId && x.Code.StartsWith("QD_"))
                    .OrderByDescending(x => x.Priority)
                    .ToListAsync(cancellationToken);
                foreach (var rule in applicableRules)
                {
                    if (await db.EvaluationConversionDetails.AnyAsync(
                            x => x.EvaluationId == evaluation.Id &&
                                 x.ProductId == approved.Product.Id &&
                                 x.Unit == rule.Unit,
                            cancellationToken))
                        continue;

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
    /// 8 bộ môn, 32 giảng viên, 3 năm học, 80 kế hoạch và hơn 150 sản phẩm.
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
                i % 2 == 0 ? "NAM" : "NU", $"gv{ordinal:D2}@huit.edu.vn", $"0902{ordinal:D6}",
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

    /// <summary>Bổ sung người quản lý cho mọi khoa/bộ môn của bộ dữ liệu lớn.</summary>
    private static async Task EnsureUnitManagersAsync(ApplicationDbContext db, IPasswordService passwordService, CancellationToken ct)
    {
        var accounts = new List<Account>();
        var dataFaculty = await db.Faculties.SingleOrDefaultAsync(x => x.Code == "KDL", ct);
        if (dataFaculty is not null && !await db.RoleAssignments.AnyAsync(x => x.Role == Roles.FacultyDean && x.FacultyId == dataFaculty.Id, ct))
        {
            var dean = new Account("truongkhoa.khdl", string.Empty, "PGS. TS. Nguyễn Đức Thành", "truongkhoa.khdl@huit.edu.vn");
            dean.SetPasswordHash(passwordService.Hash(dean, DefaultPassword));
            dean.AddRole(new RoleAssignment(Roles.FacultyDean, Scopes.Faculty, dataFaculty.Id));
            accounts.Add(dean);
        }
        var departments = await db.Departments.OrderBy(x => x.Code).ToListAsync(ct);
        foreach (var department in departments)
        {
            if (await db.RoleAssignments.AnyAsync(x => x.Role == Roles.DepartmentHead && x.DepartmentId == department.Id, ct)) continue;
            var username = $"tbm.{department.Code.ToLowerInvariant()}";
            if (await db.Accounts.AnyAsync(x => x.Username == username, ct)) continue;
            var head = new Account(username, string.Empty, $"Trưởng bộ môn {department.Name.Replace("Bộ môn ", "")}", $"{username}@huit.edu.vn");
            head.SetPasswordHash(passwordService.Hash(head, DefaultPassword));
            head.AddRole(new RoleAssignment(Roles.DepartmentHead, Scopes.Department, department.FacultyId, department.Id));
            accounts.Add(head);
        }
        // Thông báo cũ từng được gửi ngay lúc tạo phân bổ; quy tắc mới chỉ gửi sau khi khóa.
        await db.Notifications.Where(x => x.DeduplicationKey.StartsWith("PHAN_BO_CHI_TIEU:")).ExecuteDeleteAsync(ct);
        await db.Notifications
            .Where(x => x.ActionPath == null && x.DeduplicationKey.StartsWith("seed:assigned-target:"))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ActionPath, "my-targets"), ct);
        await db.Notifications
            .Where(x => x.ActionPath == null &&
                        (x.DeduplicationKey.StartsWith("seed:due:") || x.DeduplicationKey.StartsWith("seed:overdue:")))
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ActionPath, "research-plans"), ct);
        if (accounts.Count > 0) { db.Accounts.AddRange(accounts); await db.SaveChangesAsync(ct); }
    }

    /// <summary>
    /// Nối dữ liệu quy mô lớn thành các luồng vận hành hoàn chỉnh: định mức, chỉ tiêu khoa,
    /// phân bổ bộ môn/cá nhân, bài báo gắn chỉ tiêu, trạng thái công bố, hồ sơ và thông báo.
    /// </summary>
    private static async Task EnsureOperationalDemoDataAsync(
        ApplicationDbContext db,
        long researchOfficeAccountId,
        CancellationToken cancellationToken)
    {
        var years = await db.AcademicYears.OrderBy(x => x.StartDate).ToListAsync(cancellationToken);
        var faculties = await db.Faculties.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var departments = await db.Departments.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var lecturers = await db.Lecturers.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var plans = await db.ResearchPlans.ToListAsync(cancellationToken);
        var latestYearId = years.OrderByDescending(x => x.StartDate).First().Id;

        var existingNorms = await db.ResearchNorms
            .Select(x => new { x.LecturerId, x.AcademicYearId })
            .ToListAsync(cancellationToken);
        foreach (var plan in plans)
        {
            if (existingNorms.Any(x => x.LecturerId == plan.LecturerId && x.AcademicYearId == plan.AcademicYearId))
                continue;
            var lecturer = lecturers.Single(x => x.Id == plan.LecturerId);
            var requiredHours = lecturer.AcademicRank == "PGS" ? 320m : lecturer.Degree == "TIẾN SĨ" ? 280m : 200m;
            db.ResearchNorms.Add(new ResearchNorm(
                lecturer.Id, plan.AcademicYearId, requiredHours,
                "Định mức NCKH theo học hàm, học vị và năm học."));
        }

        var classificationNames = new[]
        {
            (Name: "Xuất sắc", Minimum: 120m, Priority: 1),
            (Name: "Tốt", Minimum: 100m, Priority: 2),
            (Name: "Khá", Minimum: 80m, Priority: 3),
            (Name: "Đạt", Minimum: 50m, Priority: 4)
        };
        foreach (var year in years)
        {
            var existing = await db.EmulationClassificationRules
                .Where(x => x.AcademicYearId == year.Id)
                .Select(x => x.Name)
                .ToListAsync(cancellationToken);
            foreach (var rule in classificationNames.Where(x => !existing.Contains(x.Name)))
                db.EmulationClassificationRules.Add(new EmulationClassificationRule(
                    year.Id, rule.Name, rule.Minimum, rule.Priority));
        }
        await db.SaveChangesAsync(cancellationToken);

        var catalogs = await db.JournalConferences.OrderBy(x => x.Id).ToListAsync(cancellationToken);
        var internationalCatalog = catalogs.FirstOrDefault(x => x.Index == "SCOPUS");
        var domesticCatalog = catalogs.FirstOrDefault(x => x.Index == "TRONG_NUOC");
        var articleTypeId = await db.ProductTypes
            .Where(x => x.Code == "BAI_BAO")
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);

        foreach (var year in years)
        foreach (var faculty in faculties)
        {
            var facultyDepartmentIds = departments
                .Where(x => x.FacultyId == faculty.Id)
                .Select(x => x.Id)
                .ToHashSet();
            var facultyPlans = plans
                .Where(x => x.AcademicYearId == year.Id &&
                            facultyDepartmentIds.Contains(lecturers.Single(l => l.Id == x.LecturerId).DepartmentId))
                .ToList();
            if (facultyPlans.Count == 0) continue;

            var target = await db.ResearchTargets.FirstOrDefaultAsync(
                x => x.FacultyId == faculty.Id && x.AcademicYearId == year.Id,
                cancellationToken);
            if (target is null)
            {
                target = new ResearchTarget(faculty.Id, year.Id, facultyPlans.Count, year.EndDate.AddDays(-30));
                target.MarkAssigned(researchOfficeAccountId);
                target.Accept(researchOfficeAccountId, new DateTimeOffset(year.StartDate.ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero));
                target.Publish(new DateTimeOffset(year.StartDate.AddDays(14).ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero));
                db.ResearchTargets.Add(target);
                await db.SaveChangesAsync(cancellationToken);
            }

            var targetAllocations = await db.ResearchTargetAllocations
                .Where(x => x.ResearchTargetId == target.Id)
                .ToListAsync(cancellationToken);
            var existingPersonalAllocations = await (from personal in db.LecturerTargetAllocations
                                                     join allocation in db.ResearchTargetAllocations
                                                         on personal.DepartmentAllocationId equals allocation.Id
                                                     where allocation.ResearchTargetId == target.Id
                                                     select personal).ToListAsync(cancellationToken);
            var plannedFacultyTotal = facultyPlans.Sum(plan =>
            {
                var existing = existingPersonalAllocations
                    .Where(x => x.LecturerId == plan.LecturerId)
                    .Sum(x => x.AllocatedArticleCount);
                return existing > 0 ? existing : 1;
            });
            target.Update(plannedFacultyTotal, year.EndDate.AddDays(-30));
            await db.SaveChangesAsync(cancellationToken);

            foreach (var department in departments.Where(x => x.FacultyId == faculty.Id))
            {
                var departmentLecturerIds = lecturers
                    .Where(x => x.DepartmentId == department.Id &&
                                facultyPlans.Any(p => p.LecturerId == x.Id))
                    .Select(x => x.Id)
                    .ToList();
                if (departmentLecturerIds.Count == 0) continue;

                var allocation = targetAllocations.FirstOrDefault(x => x.DepartmentId == department.Id);
                if (allocation is null)
                {
                    allocation = new ResearchTargetAllocation(
                        target.Id, faculty.Id, department.Id, departmentLecturerIds.Count,
                        year.StartDate.AddDays(21));
                    db.ResearchTargetAllocations.Add(allocation);
                    targetAllocations.Add(allocation);
                    await db.SaveChangesAsync(cancellationToken);
                }

                var personalAllocations = await db.LecturerTargetAllocations
                    .Where(x => x.DepartmentAllocationId == allocation.Id)
                    .ToListAsync(cancellationToken);
                var missingLecturerIds = departmentLecturerIds
                    .Where(id => personalAllocations.All(x => x.LecturerId != id))
                    .ToList();
                var plannedDepartmentTotal = personalAllocations.Sum(x => x.AllocatedArticleCount) +
                                             missingLecturerIds.Count;
                allocation.Update(plannedDepartmentTotal, year.StartDate.AddDays(21));
                await db.SaveChangesAsync(cancellationToken);

                foreach (var lecturerId in missingLecturerIds)
                {
                    var personal = new LecturerTargetAllocation(
                        allocation.Id, lecturerId, 1, year.StartDate.AddDays(24));
                    db.LecturerTargetAllocations.Add(personal);
                    personalAllocations.Add(personal);
                }
                await db.SaveChangesAsync(cancellationToken);
            }

            target.Update(targetAllocations.Sum(x => x.AllocatedArticleCount), year.EndDate.AddDays(-30));
            if (target.Status == "CHO_TIEP_NHAN")
                target.Accept(researchOfficeAccountId, DateTimeOffset.UtcNow);
            if (target.Status == "DA_TIEP_NHAN")
                target.Publish(DateTimeOffset.UtcNow);
            if (target.Status == "CONG_BO")
                target.Lock(new DateTimeOffset(year.StartDate.AddDays(30).ToDateTime(new TimeOnly(8, 0)), TimeSpan.Zero));
            await db.SaveChangesAsync(cancellationToken);

            if (year.Id == latestYearId)
            {
                var deanAccountIds = await db.RoleAssignments.AsNoTracking()
                    .Where(x => x.Role == Roles.FacultyDean && x.FacultyId == faculty.Id)
                    .Select(x => x.AccountId)
                    .Distinct()
                    .ToListAsync(cancellationToken);
                foreach (var accountId in deanAccountIds)
                {
                    var key = $"CHI_TIEU_CHO_TIEP_NHAN:{target.Id}:{accountId}";
                    if (!await db.Notifications.AnyAsync(x => x.DeduplicationKey == key, cancellationToken))
                        db.Notifications.Add(Sent(new Notification(
                            accountId,
                            "HE_THONG",
                            $"{faculty.Name} được Phòng QLKH giao {target.AssignedArticleCount} bài báo trong năm học {year.Code}. Chỉ tiêu đã được tiếp nhận và phân bổ.",
                            key,
                            actionPath: "research-targets")));
                }

                foreach (var allocation in targetAllocations)
                {
                    var headAccountIds = await db.RoleAssignments.AsNoTracking()
                        .Where(x => x.Role == Roles.DepartmentHead && x.DepartmentId == allocation.DepartmentId)
                        .Select(x => x.AccountId)
                        .Distinct()
                        .ToListAsync(cancellationToken);
                    var departmentName = departments.Single(x => x.Id == allocation.DepartmentId).Name;
                    foreach (var accountId in headAccountIds)
                    {
                        var key = $"PHAN_BO_DA_KHOA:{target.Id}:{allocation.Id}";
                        if (!await db.Notifications.AnyAsync(x => x.RecipientAccountId == accountId && x.DeduplicationKey == key, cancellationToken))
                            db.Notifications.Add(Sent(new Notification(
                                accountId,
                                "HE_THONG",
                                $"Phân bổ chỉ tiêu NCKH cho {departmentName} đã được khóa: {allocation.AllocatedArticleCount} bài. Bạn có thể phân công cho giảng viên.",
                                key,
                                actionPath: "research-targets")));
                    }
                }
                await db.SaveChangesAsync(cancellationToken);
            }

            var articleProducts = await (from product in db.Products
                                         join item in db.ResearchPlanItems on product.ResearchPlanItemId equals item.Id
                                         join plan in db.ResearchPlans on item.ResearchPlanId equals plan.Id
                                         join lecturer in db.Lecturers on plan.LecturerId equals lecturer.Id
                                         where plan.AcademicYearId == year.Id &&
                                               facultyDepartmentIds.Contains(lecturer.DepartmentId) &&
                                               item.ProductTypeId == articleTypeId
                                         select product).ToListAsync(cancellationToken);
            foreach (var product in articleProducts)
            {
                var catalog = product.JournalIndex == "SCOPUS" ? internationalCatalog : domesticCatalog;
                if (!product.ResearchTargetId.HasValue)
                    product.LinkTarget(target.Id, catalog?.Id);

                var articleStatus = product.Status switch
                {
                    ProductStatuses.Approved => "DA_XUAT_BAN",
                    ProductStatuses.ResearchOfficeApproved or ProductStatuses.FacultyDeanApproved => "DA_NHAN_DANG",
                    ProductStatuses.DepartmentHeadApproved or ProductStatuses.Submitted => "DANG_PHAN_BIEN",
                    _ => "DANG_VIET"
                };
                DateOnly? publishedDate = articleStatus == "DA_XUAT_BAN"
                    ? product.PublishedDate is { } existingDate &&
                      existingDate >= year.StartDate && existingDate <= year.EndDate
                        ? existingDate
                        : year.EndDate.AddDays(-120)
                    : null;
                product.Update(product.Title, product.Description, product.PublicationInfo,
                    publishedDate, articleStatus, product.JournalIndex, product.JournalClassification, product.ProjectLevel);
            }

            foreach (var plan in facultyPlans.Where(x => x.AcademicYearId == latestYearId))
            {
                var lecturer = lecturers.Single(x => x.Id == plan.LecturerId);
                var key = $"seed:assigned-target:{target.Id}:lecturer:{lecturer.Id}";
                if (!await db.Notifications.AnyAsync(x => x.DeduplicationKey == key, cancellationToken))
                    db.Notifications.Add(Sent(new Notification(
                        lecturer.AccountId, "HE_THONG",
                        $"Bạn đã được giao chỉ tiêu bài báo năm học {year.Code}. Hãy liên kết sản phẩm trong kế hoạch để theo dõi tiến độ.",
                        key, researchPlanId: plan.Id, actionPath: "my-targets")));
            }
            await db.SaveChangesAsync(cancellationToken);
        }

        var profiles = await db.ScientificProfiles.ToListAsync(cancellationToken);
        foreach (var profile in profiles.Where(x => string.IsNullOrWhiteSpace(x.DetailsJson) || x.DetailsJson == "{}"))
        {
            var lecturer = lecturers.Single(x => x.Id == profile.LecturerId);
            var degreeLevel = lecturer.Degree == "TIẾN SĨ" ? "TIEN_SI" : "THAC_SI";
            var graduationYear = lecturer.Degree == "TIẾN SĨ" ? 2018 : 2016;
            var details = new ScientificProfileDetails
            {
                BirthPlace = "Thành phố Hồ Chí Minh",
                Hometown = "Việt Nam",
                Ethnicity = "Kinh",
                HighestDegreeYear = graduationYear,
                HighestDegreeCountry = "Việt Nam",
                WorkUnit = "Trường Đại học Công Thương Thành phố Hồ Chí Minh",
                ContactAddress = "140 Lê Trọng Tấn, phường Tây Thạnh, Thành phố Hồ Chí Minh",
                Education =
                [
                    new EducationEntry
                    {
                        Level = "DAI_HOC", TrainingMode = "Chính quy",
                        Institution = "Trường đại học công lập tại Việt Nam",
                        Specialization = "Công nghệ thông tin", Country = "Việt Nam", GraduationYear = 2008
                    },
                    new EducationEntry
                    {
                        Level = degreeLevel, TrainingMode = "Chính quy",
                        Institution = "Đại học Quốc gia Thành phố Hồ Chí Minh",
                        Specialization = "Khoa học máy tính", Country = "Việt Nam", GraduationYear = graduationYear,
                        ThesisTitle = "Nghiên cứu ứng dụng dữ liệu và chuyển đổi số trong giáo dục"
                    }
                ],
                Languages =
                [
                    new LanguageEntry { Name = "Tiếng Anh", Proficiency = "Đáp ứng chuẩn ngoại ngữ giảng viên đại học" }
                ],
                Employment =
                [
                    new EmploymentEntry
                    {
                        FromDate = new DateOnly(2012, 9, 1), ToDate = new DateOnly(2026, 11, 10),
                        Organization = "Trường Đại học Công Thương Thành phố Hồ Chí Minh",
                        Position = lecturer.Position ?? "Giảng viên"
                    }
                ],
                ResearchProjects =
                [
                    new ResearchProjectEntry
                    {
                        Title = $"Ứng dụng chuyển đổi số trong lĩnh vực chuyên môn của {lecturer.FullName}",
                        StartYear = 2024, EndYear = 2026, Level = "Cấp trường", Responsibility = "Thành viên"
                    }
                ],
                Publications =
                [
                    new PublicationEntry
                    {
                        Title = "Giải pháp dữ liệu hỗ trợ quản trị và đào tạo đại học",
                        Year = 2025, Journal = "Tạp chí Khoa học và Công nghệ"
                    }
                ],
                DeclarationPlace = "Thành phố Hồ Chí Minh",
                DeclarationDate = new DateOnly(2026, 11, 10)
            };
            profile.UpdateDetails(JsonSerializer.Serialize(details, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task ClearBlankLecturerResearchDataAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var blankCodes = new[] { "CNTT001", "CNTT002", "CNTT003", "CNTT004", "CNTT005" };
        var blankLecturers = await db.Lecturers
            .Where(x => blankCodes.Contains(x.Code))
            .ToListAsync(cancellationToken);
        var blankIds = blankLecturers.Select(x => x.Id).ToHashSet();
        if (blankIds.Count == 0) return;

        var coAuthors = await db.ProductCoAuthors
            .Where(x => blankIds.Contains(x.LecturerId))
            .ToListAsync(cancellationToken);
        db.ProductCoAuthors.RemoveRange(coAuthors);

        var profiles = await db.ScientificProfiles
            .Where(x => blankIds.Contains(x.LecturerId))
            .ToListAsync(cancellationToken);
        foreach (var profile in profiles)
        {
            if (string.IsNullOrWhiteSpace(profile.DetailsJson) || profile.DetailsJson == "{}") continue;
            try
            {
                var details = JsonSerializer.Deserialize<ScientificProfileDetails>(
                    profile.DetailsJson,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web));
                if (details is null) continue;
                details.ResearchProjects.Clear();
                details.Publications.Clear();
                profile.UpdateDetails(JsonSerializer.Serialize(
                    details,
                    new JsonSerializerOptions(JsonSerializerDefaults.Web)));
            }
            catch (JsonException)
            {
                profile.UpdateDetails("{}");
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureNguyenMinhAnhProfileAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var lecturerId = await (from lecturer in db.Lecturers
                                join account in db.Accounts on lecturer.AccountId equals account.Id
                                where account.Username == "gv.nguyenminhanh"
                                select (long?)lecturer.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (!lecturerId.HasValue) return;

        var profile = await db.ScientificProfiles
            .SingleOrDefaultAsync(x => x.LecturerId == lecturerId.Value, cancellationToken);
        if (profile is null)
        {
            profile = new ScientificProfile(
                lecturerId.Value,
                "Kỹ thuật phần mềm; học máy ứng dụng; phân tích dữ liệu giáo dục",
                "Trí tuệ nhân tạo, khai phá dữ liệu và công nghệ giáo dục",
                "Mô hình dự báo nguy cơ học vụ; phân tích học tập; hệ thống hỗ trợ ra quyết định trong giáo dục",
                "Giảng viên chính, chủ nhiệm và thành viên nhiều đề tài về trí tuệ nhân tạo trong giáo dục; có công bố trong nước và quốc tế.",
                DateTimeOffset.UtcNow);
            db.ScientificProfiles.Add(profile);
        }
        else
        {
            profile.Update(
                "Kỹ thuật phần mềm; học máy ứng dụng; phân tích dữ liệu giáo dục",
                "Trí tuệ nhân tạo, khai phá dữ liệu và công nghệ giáo dục",
                "Mô hình dự báo nguy cơ học vụ; phân tích học tập; hệ thống hỗ trợ ra quyết định trong giáo dục",
                "Giảng viên chính, chủ nhiệm và thành viên nhiều đề tài về trí tuệ nhân tạo trong giáo dục; có công bố trong nước và quốc tế.",
                DateTimeOffset.UtcNow);
        }

        ApplyNguyenMinhAnhProfileDetails(profile);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void ApplyNguyenMinhAnhProfileDetails(ScientificProfile profile)
    {
        var details = new ScientificProfileDetails
        {
            BirthPlace = "Thành phố Hồ Chí Minh",
            Hometown = "Tỉnh Bến Tre",
            Ethnicity = "Kinh",
            HighestDegreeYear = 2018,
            HighestDegreeCountry = "Việt Nam",
            WorkUnit = "Khoa Công nghệ Thông tin, Trường Đại học Công Thương Thành phố Hồ Chí Minh",
            ContactAddress = "140 Lê Trọng Tấn, phường Tây Thạnh, Thành phố Hồ Chí Minh",
            OfficePhone = "028 3816 3319",
            IdentityNumber = "079085000001",
            IdentityIssuedDate = new DateOnly(2021, 4, 15),
            IdentityIssuedPlace = "Cục Cảnh sát quản lý hành chính về trật tự xã hội",
            Education =
            [
                new EducationEntry
                {
                    Level = "DAI_HOC", TrainingMode = "Chính quy",
                    Institution = "Trường Đại học Khoa học Tự nhiên, ĐHQG TP.HCM",
                    Specialization = "Công nghệ thông tin", Country = "Việt Nam", GraduationYear = 2007
                },
                new EducationEntry
                {
                    Level = "THAC_SI", TrainingMode = "Chính quy",
                    Institution = "Trường Đại học Khoa học Tự nhiên, ĐHQG TP.HCM",
                    Specialization = "Khoa học máy tính", Country = "Việt Nam", GraduationYear = 2011,
                    ThesisTitle = "Khai phá dữ liệu hỗ trợ dự báo kết quả học tập"
                },
                new EducationEntry
                {
                    Level = "TIEN_SI", TrainingMode = "Chính quy",
                    Institution = "Trường Đại học Bách khoa, ĐHQG TP.HCM",
                    Specialization = "Khoa học máy tính", Country = "Việt Nam", GraduationYear = 2018,
                    ThesisTitle = "Mô hình học máy thích nghi trong phân tích dữ liệu giáo dục"
                }
            ],
            Languages =
            [
                new LanguageEntry { Name = "Tiếng Anh", Proficiency = "Bậc 5/6 theo Khung năng lực ngoại ngữ Việt Nam" }
            ],
            Employment =
            [
                new EmploymentEntry
                {
                    FromDate = new DateOnly(2008, 9, 1), ToDate = new DateOnly(2014, 8, 31),
                    Organization = "Khoa Công nghệ Thông tin, Trường Đại học Công nghiệp Thực phẩm TP.HCM",
                    Position = "Giảng viên"
                },
                new EmploymentEntry
                {
                    FromDate = new DateOnly(2014, 9, 1), ToDate = new DateOnly(2026, 11, 10),
                    Organization = "Khoa Công nghệ Thông tin, Trường Đại học Công Thương Thành phố Hồ Chí Minh",
                    Position = "Giảng viên chính; phụ trách nhóm nghiên cứu phân tích dữ liệu giáo dục"
                }
            ],
            ResearchProjects =
            [
                new ResearchProjectEntry
                {
                    Title = "Trợ lý học tập thông minh cho sinh viên",
                    StartYear = 2026, EndYear = 2027, Level = "Cấp trường", Responsibility = "Chủ nhiệm đề tài"
                }
            ],
            Publications =
            [
                new PublicationEntry
                {
                    Title = "Mô hình dự báo nguy cơ học vụ bằng học máy",
                    Year = 2026, Journal = "Bài báo · Tạp chí Khoa học Giáo dục Việt Nam"
                },
                new PublicationEntry
                {
                    Title = "Báo cáo kết quả nghiên cứu tại Hội nghị FAIR",
                    Year = 2026, Journal = "Bài/kỷ yếu/báo cáo hội nghị, hội thảo · Hội nghị FAIR"
                }
            ],
            DeclarationPlace = "Thành phố Hồ Chí Minh",
            DeclarationDate = new DateOnly(2026, 11, 10)
        };

        profile.UpdateDetails(JsonSerializer.Serialize(
            details,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    /// <summary>
    /// Giảng viên dành riêng cho kịch bản demo luồng tạo mới năm 2026-2027.
    /// Có đầy đủ hồ sơ khoa học nhưng cố ý chưa tạo kế hoạch/cam kết để người demo
    /// bắt đầu nghiệp vụ ngay từ bước đăng ký kế hoạch đầu năm.
    /// </summary>
    private static async Task EnsureUncommittedDemoLecturerAsync(
        ApplicationDbContext db,
        IPasswordService passwordService,
        CancellationToken cancellationToken)
    {
        const string username = "demo.gv2026";
        const string lecturerCode = "DEMO2026";

        var account = await db.Accounts
            .Include(x => x.RoleAssignments)
            .SingleOrDefaultAsync(x => x.Username == username, cancellationToken);

        if (account is null)
        {
            account = new Account(
                username,
                string.Empty,
                "ThS. Nguyễn Hoài Phương",
                "hoaiphuong.demo@huit.edu.vn");
            account.SetPasswordHash(passwordService.Hash(account, DefaultPassword));
            account.AddRole(new RoleAssignment(Roles.Lecturer, Scopes.Personal));
            db.Accounts.Add(account);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (!account.RoleAssignments.Any(x => x.Role == Roles.Lecturer))
        {
            account.AddRole(new RoleAssignment(Roles.Lecturer, Scopes.Personal));
        }

        var softwareDepartmentId = await db.Departments
            .Where(x => x.Code == "CNPM")
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);

        var lecturer = await db.Lecturers
            .SingleOrDefaultAsync(x => x.AccountId == account.Id, cancellationToken);
        if (lecturer is null)
        {
            lecturer = new Lecturer(
                softwareDepartmentId,
                account.Id,
                lecturerCode,
                "ThS. Nguyễn Hoài Phương",
                new DateOnly(1991, 4, 18),
                "NU",
                "hoaiphuong.demo@huit.edu.vn",
                "0909122026",
                null,
                "THẠC SĨ",
                "Giảng viên");
            db.Lecturers.Add(lecturer);
            await db.SaveChangesAsync(cancellationToken);
        }

        var profile = await db.ScientificProfiles
            .SingleOrDefaultAsync(x => x.LecturerId == lecturer.Id, cancellationToken);
        if (profile is null)
        {
            profile = new ScientificProfile(
                lecturer.Id,
                "Kỹ thuật phần mềm; kiểm thử phần mềm; phân tích dữ liệu",
                "Kỹ thuật phần mềm và công nghệ giáo dục",
                "Tự động hóa kiểm thử; phân tích học tập; ứng dụng trí tuệ nhân tạo trong giáo dục",
                "Giảng viên Bộ môn Công nghệ phần mềm, tham gia nghiên cứu và chuyển giao các giải pháp số phục vụ đào tạo đại học.",
                DateTimeOffset.UtcNow);
            db.ScientificProfiles.Add(profile);
        }

        var details = new ScientificProfileDetails
        {
            BirthPlace = "Thành phố Hồ Chí Minh",
            Hometown = "Tỉnh Đồng Nai",
            Ethnicity = "Kinh",
            HighestDegreeYear = 2018,
            HighestDegreeCountry = "Việt Nam",
            WorkUnit = "Khoa Công nghệ Thông tin, Trường Đại học Công Thương Thành phố Hồ Chí Minh",
            ContactAddress = "140 Lê Trọng Tấn, phường Tây Thạnh, Thành phố Hồ Chí Minh",
            OfficePhone = "028 3816 3319",
            IdentityNumber = "079191020026",
            IdentityIssuedDate = new DateOnly(2021, 6, 15),
            IdentityIssuedPlace = "Cục Cảnh sát quản lý hành chính về trật tự xã hội",
            Education =
            [
                new EducationEntry
                {
                    Level = "DAI_HOC", TrainingMode = "Chính quy",
                    Institution = "Trường Đại học Công nghệ Thông tin, ĐHQG TP.HCM",
                    Specialization = "Kỹ thuật phần mềm", Country = "Việt Nam", GraduationYear = 2013
                },
                new EducationEntry
                {
                    Level = "THAC_SI", TrainingMode = "Chính quy",
                    Institution = "Trường Đại học Bách khoa, ĐHQG TP.HCM",
                    Specialization = "Khoa học máy tính", Country = "Việt Nam", GraduationYear = 2018,
                    ThesisTitle = "Mô hình kiểm thử tự động cho các hệ thống thông tin giáo dục"
                }
            ],
            Languages =
            [
                new LanguageEntry
                {
                    Name = "Tiếng Anh",
                    Proficiency = "Bậc 4/6 theo Khung năng lực ngoại ngữ Việt Nam"
                }
            ],
            Employment =
            [
                new EmploymentEntry
                {
                    FromDate = new DateOnly(2014, 9, 1), ToDate = new DateOnly(2018, 8, 31),
                    Organization = "Công ty TNHH Giải pháp Phần mềm Giáo dục",
                    Position = "Kỹ sư kiểm thử phần mềm"
                },
                new EmploymentEntry
                {
                    FromDate = new DateOnly(2018, 9, 1), ToDate = new DateOnly(2026, 11, 11),
                    Organization = "Trường Đại học Công Thương Thành phố Hồ Chí Minh",
                    Position = "Giảng viên Bộ môn Công nghệ phần mềm"
                }
            ],
            ResearchProjects =
            [
                new ResearchProjectEntry
                {
                    Title = "Hệ thống hỗ trợ kiểm thử tự động cho cổng dịch vụ sinh viên",
                    StartYear = 2022, EndYear = 2023, Level = "Cấp trường", Responsibility = "Thành viên"
                },
                new ResearchProjectEntry
                {
                    Title = "Phân tích dữ liệu học tập phục vụ cảnh báo sớm kết quả học vụ",
                    StartYear = 2024, EndYear = 2026, Level = "Cấp trường", Responsibility = "Thành viên chính"
                }
            ],
            Publications =
            [
                new PublicationEntry
                {
                    Title = "A Test Automation Framework for University Information Systems",
                    Year = 2024, Journal = "International Conference on Information Technology"
                },
                new PublicationEntry
                {
                    Title = "Ứng dụng phân tích dữ liệu trong cảnh báo sớm kết quả học tập",
                    Year = 2025, Journal = "Tạp chí Khoa học và Công nghệ"
                }
            ],
            DeclarationPlace = "Thành phố Hồ Chí Minh",
            DeclarationDate = new DateOnly(2026, 11, 11)
        };
        profile.UpdateDetails(JsonSerializer.Serialize(
            details,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        var academicYearId = await db.AcademicYears
            .Where(x => x.Code == "2026-2027")
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);
        if (!await db.ResearchNorms.AnyAsync(x =>
                x.LecturerId == lecturer.Id && x.AcademicYearId == academicYearId,
                cancellationToken))
        {
            db.ResearchNorms.Add(new ResearchNorm(
                lecturer.Id, academicYearId, 200m,
                "Định mức NCKH giảng viên năm học 2026-2027", 100m));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Hồ sơ giảng viên có tên đăng nhập và thông tin giống dữ liệu vận hành thật,
    /// nhưng chưa lập kế hoạch/cam kết năm học để phục vụ kiểm thử lặp lại.
    /// </summary>
    private static async Task EnsureUncommittedNguyenAnhThuAsync(
        ApplicationDbContext db,
        IPasswordService passwordService,
        CancellationToken cancellationToken)
    {
        const string username = "gv.nguyenanhthu";

        var account = await db.Accounts
            .Include(x => x.RoleAssignments)
            .SingleOrDefaultAsync(x => x.Username == username, cancellationToken);
        if (account is null)
        {
            account = new Account(
                username,
                string.Empty,
                "ThS. Nguyễn Anh Thư",
                "nguyenanhthu@huit.edu.vn");
            account.SetPasswordHash(passwordService.Hash(account, DefaultPassword));
            account.AddRole(new RoleAssignment(Roles.Lecturer, Scopes.Personal));
            db.Accounts.Add(account);
            await db.SaveChangesAsync(cancellationToken);
        }
        else if (!account.RoleAssignments.Any(x => x.Role == Roles.Lecturer))
        {
            account.AddRole(new RoleAssignment(Roles.Lecturer, Scopes.Personal));
        }

        var softwareDepartmentId = await db.Departments
            .Where(x => x.Code == "CNPM")
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);
        var lecturer = await db.Lecturers
            .SingleOrDefaultAsync(x => x.AccountId == account.Id, cancellationToken);
        if (lecturer is null)
        {
            lecturer = new Lecturer(
                softwareDepartmentId,
                account.Id,
                "CNTT009",
                "ThS. Nguyễn Anh Thư",
                new DateOnly(1990, 10, 22),
                "NU",
                "nguyenanhthu@huit.edu.vn",
                "0918221090",
                null,
                "THẠC SĨ",
                "Giảng viên");
            db.Lecturers.Add(lecturer);
            await db.SaveChangesAsync(cancellationToken);
        }

        var profile = await db.ScientificProfiles
            .SingleOrDefaultAsync(x => x.LecturerId == lecturer.Id, cancellationToken);
        if (profile is null)
        {
            profile = new ScientificProfile(
                lecturer.Id,
                "Kỹ thuật phần mềm; thiết kế trải nghiệm người dùng; kiểm thử phần mềm; phân tích yêu cầu",
                "Kỹ thuật phần mềm, hệ thống thông tin và công nghệ giáo dục",
                "Phát triển phần mềm hướng người dùng; tự động hóa kiểm thử; quản lý yêu cầu; chuyển đổi số giáo dục",
                "Giảng viên Bộ môn Công nghệ phần mềm; chủ nhiệm và thành viên các đề tài về trải nghiệm người dùng, quản lý yêu cầu và bảo đảm chất lượng phần mềm; có công bố hội nghị và tạp chí trong nước, quốc tế.",
                DateTimeOffset.UtcNow);
            db.ScientificProfiles.Add(profile);
        }
        else
        {
            profile.Update(
                "Kỹ thuật phần mềm; thiết kế trải nghiệm người dùng; kiểm thử phần mềm; phân tích yêu cầu",
                "Kỹ thuật phần mềm, hệ thống thông tin và công nghệ giáo dục",
                "Phát triển phần mềm hướng người dùng; tự động hóa kiểm thử; quản lý yêu cầu; chuyển đổi số giáo dục",
                "Giảng viên Bộ môn Công nghệ phần mềm; chủ nhiệm và thành viên các đề tài về trải nghiệm người dùng, quản lý yêu cầu và bảo đảm chất lượng phần mềm; có công bố hội nghị và tạp chí trong nước, quốc tế.",
                DateTimeOffset.UtcNow);
        }

        var details = new ScientificProfileDetails
        {
            BirthPlace = "Tỉnh Bình Dương",
            Hometown = "Tỉnh Bình Dương",
            Ethnicity = "Kinh",
            HighestDegreeYear = 2017,
            HighestDegreeCountry = "Việt Nam",
            WorkUnit = "Khoa Công nghệ Thông tin, Trường Đại học Công Thương Thành phố Hồ Chí Minh",
            ContactAddress = "140 Lê Trọng Tấn, phường Tây Thạnh, Thành phố Hồ Chí Minh",
            OfficePhone = "028 3816 3319",
            HomePhone = "0274 382 2026",
            Fax = "028 3816 3320",
            IdentityNumber = "074190010009",
            IdentityIssuedDate = new DateOnly(2021, 8, 12),
            IdentityIssuedPlace = "Cục Cảnh sát quản lý hành chính về trật tự xã hội",
            Education =
            [
                new EducationEntry
                {
                    Level = "DAI_HOC", TrainingMode = "Chính quy",
                    Institution = "Trường Đại học Công nghệ Thông tin, ĐHQG TP.HCM",
                    Specialization = "Kỹ thuật phần mềm", Country = "Việt Nam", GraduationYear = 2012
                },
                new EducationEntry
                {
                    Level = "THAC_SI", TrainingMode = "Chính quy",
                    Institution = "Trường Đại học Khoa học Tự nhiên, ĐHQG TP.HCM",
                    Specialization = "Công nghệ thông tin", Country = "Việt Nam", GraduationYear = 2017,
                    ThesisTitle = "Phương pháp đánh giá khả dụng cho hệ thống thông tin trên nền tảng web"
                }
            ],
            Languages =
            [
                new LanguageEntry
                {
                    Name = "Tiếng Anh",
                    Proficiency = "Bậc 4/6 theo Khung năng lực ngoại ngữ Việt Nam"
                },
                new LanguageEntry
                {
                    Name = "Tiếng Nhật",
                    Proficiency = "JLPT N3"
                }
            ],
            Employment =
            [
                new EmploymentEntry
                {
                    FromDate = new DateOnly(2012, 9, 1), ToDate = new DateOnly(2016, 8, 31),
                    Organization = "Công ty Cổ phần Giải pháp Công nghệ Việt",
                    Position = "Chuyên viên phân tích và kiểm thử phần mềm"
                },
                new EmploymentEntry
                {
                    FromDate = new DateOnly(2016, 9, 1), ToDate = new DateOnly(2021, 8, 31),
                    Organization = "Trường Đại học Công Thương Thành phố Hồ Chí Minh",
                    Position = "Giảng viên Bộ môn Công nghệ phần mềm"
                },
                new EmploymentEntry
                {
                    FromDate = new DateOnly(2021, 9, 1), ToDate = new DateOnly(2026, 11, 11),
                    Organization = "Trường Đại học Công Thương Thành phố Hồ Chí Minh",
                    Position = "Giảng viên; thành viên nhóm nghiên cứu Kỹ thuật phần mềm và Chuyển đổi số"
                }
            ],
            ResearchProjects =
            [
                new ResearchProjectEntry
                {
                    Title = "Khung đánh giá trải nghiệm người dùng cho cổng thông tin đào tạo",
                    StartYear = 2021, EndYear = 2022, Level = "Cấp trường", Responsibility = "Chủ nhiệm đề tài"
                },
                new ResearchProjectEntry
                {
                    Title = "Hệ thống quản lý yêu cầu phần mềm hỗ trợ chuyển đổi số trong trường đại học",
                    StartYear = 2024, EndYear = 2026, Level = "Cấp trường", Responsibility = "Thành viên chính"
                },
                new ResearchProjectEntry
                {
                    Title = "Nghiên cứu áp dụng kiểm thử tự động cho các hệ thống thông tin quản lý đào tạo",
                    StartYear = 2020, EndYear = 2021, Level = "Cấp khoa", Responsibility = "Chủ nhiệm đề tài"
                },
                new ResearchProjectEntry
                {
                    Title = "Nền tảng thu thập phản hồi người học hỗ trợ cải tiến chất lượng dịch vụ số",
                    StartYear = 2023, EndYear = 2024, Level = "Cấp trường", Responsibility = "Thư ký khoa học"
                }
            ],
            Publications =
            [
                new PublicationEntry
                {
                    Title = "A Usability Evaluation Model for University Web Portals",
                    Year = 2023, Journal = "International Conference on Information Systems"
                },
                new PublicationEntry
                {
                    Title = "Mô hình quản lý yêu cầu phần mềm trong các dự án chuyển đổi số giáo dục",
                    Year = 2025, Journal = "Tạp chí Khoa học và Công nghệ"
                },
                new PublicationEntry
                {
                    Title = "Automated Regression Testing for Student Information Systems",
                    Year = 2024, Journal = "International Conference on Software Engineering and Information Systems"
                },
                new PublicationEntry
                {
                    Title = "Đánh giá khả dụng của cổng thông tin đào tạo theo hướng tiếp cận lấy người dùng làm trung tâm",
                    Year = 2022, Journal = "Tạp chí Khoa học Giáo dục Việt Nam"
                },
                new PublicationEntry
                {
                    Title = "A Requirements Traceability Approach for Digital Transformation Projects",
                    Year = 2021, Journal = "Journal of Information Systems and Technology"
                },
                new PublicationEntry
                {
                    Title = "Ứng dụng kiểm thử dựa trên mô hình trong phát triển phần mềm quản lý đại học",
                    Year = 2020, Journal = "Kỷ yếu Hội nghị FAIR"
                }
            ],
            DeclarationPlace = "Thành phố Hồ Chí Minh",
            DeclarationDate = new DateOnly(2026, 11, 11)
        };
        profile.UpdateDetails(JsonSerializer.Serialize(
            details,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        var academicYearId = await db.AcademicYears
            .Where(x => x.Code == "2026-2027")
            .Select(x => x.Id)
            .SingleAsync(cancellationToken);
        if (!await db.ResearchNorms.AnyAsync(x =>
                x.LecturerId == lecturer.Id && x.AcademicYearId == academicYearId,
                cancellationToken))
        {
            db.ResearchNorms.Add(new ResearchNorm(
                lecturer.Id, academicYearId, 200m,
                "Định mức NCKH giảng viên năm học 2026-2027", 100m));
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Tạo kết quả chốt cho năm hiện hành để dashboard/báo cáo có quy mô tương xứng.</summary>
    private static async Task EnsureLargeReportEvaluationsAsync(
        ApplicationDbContext db,
        long researchOfficeAccountId,
        CancellationToken cancellationToken)
    {
        var academicYears = await db.AcademicYears.OrderBy(x => x.StartDate).ToListAsync(cancellationToken);
        // Dữ liệu demo vận hành cần kết quả đánh giá cho mọi giảng viên có sản phẩm được duyệt,
        // không chỉ nhóm seed.gv; nếu không màn hình giảng viên thật sẽ trống sau khi reset DB.
        var seedLecturers = await db.Lecturers.ToListAsync(cancellationToken);
        var articleTypeId = await db.ProductTypes.Where(x => x.Code == "BAI_BAO").Select(x => x.Id).SingleAsync(cancellationToken);
        var hourRule = await db.ConversionRules
            .Where(x => x.ProductTypeId == articleTypeId && x.Code == "QD_BAI_BAO" && x.Version == 1)
            .SingleAsync(cancellationToken);
        var pointRule = await db.ConversionRules
            .Where(x => x.ProductTypeId == articleTypeId && x.Code == "QD_DIEM_BAI_BAO" && x.Version == 1)
            .SingleAsync(cancellationToken);

        foreach (var academicYear in academicYears)
        foreach (var (lecturer, lecturerIndex) in seedLecturers.Select((value, index) => (value, index)))
        {
            var approvedLimit = int.MaxValue;
            if (lecturerIndex >= approvedLimit) continue;

            var product = await (from item in db.ResearchPlanItems
                                 join plan in db.ResearchPlans on item.ResearchPlanId equals plan.Id
                                 join candidate in db.Products on item.Id equals candidate.ResearchPlanItemId
                                 where plan.LecturerId == lecturer.Id
                                    && plan.AcademicYearId == academicYear.Id
                                    && item.ProductTypeId == articleTypeId
                                 select candidate).FirstOrDefaultAsync(cancellationToken);
            if (product is null) continue;
            // Seeder không tự công nhận sản phẩm của người dùng/demo. Chỉ tạo
            // kết quả đánh giá cho công trình đã thật sự đi qua luồng duyệt.
            if (product.Status != ProductStatuses.Approved)
                continue;

            var evaluation = await db.Evaluations.SingleOrDefaultAsync(x =>
                x.LecturerId == lecturer.Id && x.AcademicYearId == academicYear.Id && x.Attempt == 1, cancellationToken);
            if (evaluation is null)
            {
                evaluation = new Evaluation(lecturer.Id, academicYear.Id, 1, researchOfficeAccountId,
                    lecturer.Code, lecturer.FullName, lecturer.DepartmentId, lecturer.AcademicRank, lecturer.Degree);
                evaluation.Finalize(lecturer.Degree == "TIẾN SĨ" ? "HOAN_THANH_TOT" : "HOAN_THANH", "Đủ sản phẩm công bố được quy đổi theo quy định hiện hành.");
                db.Evaluations.Add(evaluation);
                await db.SaveChangesAsync(cancellationToken);
            }

            foreach (var rule in new[] { hourRule, pointRule })
            {
                if (!await db.EvaluationConversionDetails.AnyAsync(x => x.EvaluationId == evaluation.Id &&
                    x.ProductId == product.Id && x.ConversionRuleId == rule.Id, cancellationToken))
                    db.EvaluationConversionDetails.Add(new EvaluationConversionDetail(
                        evaluation.Id, lecturer.Id, academicYear.Id, product.Id, articleTypeId, rule.Id,
                        rule.ConversionValue, 1m, rule.Unit, $"Quy định {rule.Code} phiên bản {rule.Version}; tác giả chính."));
            }
        }

        // Các kết quả nền đã có từ trước cũng được bổ sung cột điểm theo cùng quy chế,
        // để báo cáo không hiển thị lẫn lộn "có giờ nhưng điểm 0" trong dữ liệu mẫu.
        var existingArticleHours = await db.EvaluationConversionDetails
            .Where(x => x.ProductTypeId == articleTypeId && x.Unit == "GIO")
            .ToListAsync(cancellationToken);
        var existingPointKeys = await db.EvaluationConversionDetails
            .Where(x => x.ProductTypeId == articleTypeId && x.Unit == "DIEM")
            .Select(x => new { x.EvaluationId, x.ProductId })
            .ToListAsync(cancellationToken);
        foreach (var hourDetail in existingArticleHours.Where(hour => !existingPointKeys.Any(point =>
                     point.EvaluationId == hour.EvaluationId && point.ProductId == hour.ProductId)))
        {
            db.EvaluationConversionDetails.Add(new EvaluationConversionDetail(
                hourDetail.EvaluationId, hourDetail.LecturerId, hourDetail.AcademicYearId, hourDetail.ProductId,
                articleTypeId, pointRule.Id, pointRule.ConversionValue, hourDetail.AuthorCoefficient,
                pointRule.Unit, $"Quy định {pointRule.Code} phiên bản {pointRule.Version}; bổ sung từ kết quả giờ NCKH."));
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
