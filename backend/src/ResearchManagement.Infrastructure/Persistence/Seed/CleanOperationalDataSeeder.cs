using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ResearchManagement.Application.Products.Abstractions;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Bộ dữ liệu vận hành v4: ngày hợp lệ, phân loại QD2023, đủ trạng thái và minh chứng thật.
/// Tài khoản, tổ chức, lý lịch, năm học và toàn bộ danh mục/quy định nền được giữ nguyên.
/// </summary>
public static class CleanOperationalDataSeeder
{
    public const string Version = "clean_operational_dataset_v4";

    public static async Task SeedAsync(ApplicationDbContext db, IFileStorage storage,
        IHostEnvironment environment, CancellationToken ct)
    {
        if (await db.Database.SqlQueryRaw<int>(
                $"SELECT COUNT(*)::int AS \"Value\" FROM nckh.ung_dung_migration WHERE ma='{Version}'")
            .SingleAsync(ct) > 0) return;

        var oldObjectKeys = await db.ProductEvidences.AsNoTracking().Select(x => x.StoredPath).ToListAsync(ct);
        var newObjectKeys = new List<string>();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.Database.ExecuteSqlRawAsync("""
                TRUNCATE TABLE
                    nckh.bao_cao_xuat, nckh.chi_tiet_quy_doi, nckh.chi_tiet_quy_doi_ung_dung,
                    nckh.dinh_muc_nckh, nckh.de_nghi_tien_do_bai_bao, nckh.dong_tac_gia,
                    nckh.ke_hoach_nckh, nckh.ket_qua_danh_gia, nckh.kiem_tra_tuong_dong,
                    nckh.lich_su_gia_han_chi_tieu, nckh.lich_su_ke_hoach, nckh.lich_su_tien_do,
                    nckh.lich_su_xet_duyet, nckh.lich_su_xet_duyet_san_pham,
                    nckh.minh_chung, nckh.minh_chung_san_pham, nckh.noi_dung_ke_hoach,
                    nckh.noi_dung_ke_hoach_san_pham, nckh.phan_bo_chi_tieu,
                    nckh.phan_cong_chi_tieu_giang_vien, nckh.phan_hoi_san_pham,
                    nckh.phan_hoi_san_pham_ung_dung, nckh.san_pham, nckh.san_pham_khoa_hoc,
                    nckh.san_pham_phien_ban, nckh.san_pham_phien_ban_ung_dung,
                    nckh.tham_gia_san_pham, nckh.thong_bao, nckh.chi_tieu_nckh
                RESTART IDENTITY CASCADE;
                """, ct);

            var types = await db.ProductTypes.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, ct);
            var typeCodes = new[] { "BAI_BAO", "DE_TAI", "SACH", "HOI_NGHI_HOI_THAO" };
            if (typeCodes.Any(code => !types.ContainsKey(code)))
                throw new InvalidOperationException("Danh mục chưa đủ bốn loại NCKH.");
            var classCodes = new Dictionary<string, string>
            {
                ["BAI_BAO"] = "QD2023_BB_SCOPUS",
                ["DE_TAI"] = "QD2023_DT_CAP_TRUONG",
                ["SACH"] = "QD2023_SACH_GIAO_TRINH",
                ["HOI_NGHI_HOI_THAO"] = "QD2023_HN_ISBN"
            };
            var classifications = await db.ProductClassifications.Where(x => x.IsActive && x.Code.StartsWith("QD2023_"))
                .ToDictionaryAsync(x => x.Code, ct);
            foreach (var code in classCodes.Values)
                if (!classifications.ContainsKey(code)) throw new InvalidOperationException($"Thiếu phân loại {code}.");

            var years = await db.AcademicYears.AsNoTracking().Where(x => x.Code == "2025-2026" || x.Code == "2026-2027")
                .OrderBy(x => x.StartDate).ToListAsync(ct);
            if (years.Count != 2) throw new InvalidOperationException("Cần năm học 2025-2026 và 2026-2027.");
            var currentYear = years.Single(x => x.Code == "2026-2027");
            var accounts = await db.Accounts.AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
            var departments = await db.Departments.AsNoTracking().ToDictionaryAsync(x => x.Id, ct);
            var lecturers = await db.Lecturers.AsNoTracking().OrderBy(x => x.Id).ToListAsync(ct);
            var demoLecturerId = lecturers.Single(x => accounts[x.AccountId].Username == "demo.gv2026").Id;
            var noCommitmentId = lecturers.Single(x => accounts[x.AccountId].Username == "gv.danghaiyen").Id;
            var draftPlanId = lecturers.Single(x => accounts[x.AccountId].Username == "gv.tranngoclan").Id;
            var activeLecturers = lecturers.Where(x => x.Id != demoLecturerId).ToList();

            var deans = await db.RoleAssignments.AsNoTracking().Where(x => x.Role == Roles.FacultyDean && x.FacultyId != null)
                .GroupBy(x => x.FacultyId!.Value).ToDictionaryAsync(x => x.Key, x => x.First().AccountId, ct);
            var heads = await db.RoleAssignments.AsNoTracking().Where(x => x.Role == Roles.DepartmentHead && x.DepartmentId != null)
                .GroupBy(x => x.DepartmentId!.Value).ToDictionaryAsync(x => x.Key, x => x.First().AccountId, ct);
            var adminId = await db.RoleAssignments.AsNoTracking().Where(x => x.Role == Roles.Admin).Select(x => x.AccountId).FirstAsync(ct);
            var journals = await db.JournalConferences.AsNoTracking().ToListAsync(ct);
            var articleVenue = journals.FirstOrDefault(x => x.Type == "TAP_CHI");
            var conferenceVenue = journals.FirstOrDefault(x => x.Type == "HOI_NGHI");
            var evidenceSource = Path.Combine(environment.ContentRootPath, "DemoEvidence", "gritnet-student-performance.pdf");
            if (!File.Exists(evidenceSource)) throw new FileNotFoundException("Thiếu tệp minh chứng seed.", evidenceSource);
            var evidenceInfo = new FileInfo(evidenceSource);

            var productRecords = new List<(Product Product, ResearchPlanItem Item, string TypeCode,
                AcademicYear Year, Lecturer Lecturer, string DesiredStatus, long OwnerAccountId,
                long ReviewerHeadId, long ReviewerDeanId)>();

            foreach (var year in years)
            foreach (var (lecturer, lecturerIndex) in activeLecturers.Select((value, index) => (value, index)))
            {
                if (year.Id == currentYear.Id && lecturer.Id == noCommitmentId) continue;
                var isDraftPlan = year.Id == currentYear.Id && lecturer.Id == draftPlanId;
                var plan = new ResearchPlan(lecturer.Id, year.Id, isDraftPlan ? 2 : 4,
                    year.StartDate.AddDays(20 + lecturerIndex % 7), year.EndDate.AddDays(-30));
                if (!isDraftPlan) plan.Submit();
                db.ResearchPlans.Add(plan);
                await db.SaveChangesAsync(ct);

                var codes = isDraftPlan ? typeCodes.Take(2) : typeCodes;
                foreach (var (typeCode, typeIndex) in codes.Select((value, index) => (value, index)))
                {
                    var item = new ResearchPlanItem(plan.Id, types[typeCode].Id,
                        ItemName(typeCode, lecturer.FullName, year.Code), 1, year.EndDate.AddDays(-45 + typeIndex * 3));
                    db.ResearchPlanItems.Add(item);
                    await db.SaveChangesAsync(ct);
                    if (isDraftPlan) continue;

                    var status = year.Code == "2025-2026" ? ProductStatuses.Approved :
                        CurrentStatus((lecturerIndex + typeIndex) % 4, typeCode);
                    var approved = status == ProductStatuses.Approved;
                    var date = approved ? RecognitionDate(typeCode, year.Code) : null;
                    var articleStatus = typeCode == "BAI_BAO" ? status switch
                    {
                        ProductStatuses.Approved => "DA_XUAT_BAN",
                        ProductStatuses.DepartmentHeadApproved => "DANG_PHAN_BIEN",
                        _ => "DANG_VIET"
                    } : null;
                    var publication = Publication(typeCode, articleVenue?.Name, conferenceVenue?.Name);
                    var product = new Product(item.Id, lecturer.Id, ProductTitle(typeCode, lecturer.FullName, year.Code),
                        "Hồ sơ minh họa được tạo theo quy trình NCKH cấp khoa, có ngày và phân loại hợp lệ.",
                        publication, date, articleStatus,
                        typeCode == "BAI_BAO" ? "SCOPUS" : null,
                        typeCode == "BAI_BAO" ? "Q2" : null,
                        typeCode == "DE_TAI" ? "CẤP_TRƯỜNG" : null,
                        typeCode == "BAI_BAO" ? articleVenue?.Id : typeCode == "HOI_NGHI_HOI_THAO" ? conferenceVenue?.Id : null);
                    product.ClassificationId = classifications[classCodes[typeCode]].Id;
                    ApplyStatus(product, status);
                    item.UpdateProgress(approved ? "DA_THUC_HIEN" : status == ProductStatuses.Draft ? "CHUA_THUC_HIEN" : "DANG_THUC_HIEN",
                        approved ? "Đã được công nhận và quy đổi." : null,
                        status == ProductStatuses.Returned ? "Bổ sung minh chứng theo phản hồi." : null);
                    db.Products.Add(product);
                    await db.SaveChangesAsync(ct);

                    var department = departments[lecturer.DepartmentId];
                    productRecords.Add((product, item, typeCode, year, lecturer, status, lecturer.AccountId,
                        heads.GetValueOrDefault(lecturer.DepartmentId, deans.GetValueOrDefault(department.FacultyId, adminId)),
                        deans.GetValueOrDefault(department.FacultyId, adminId)));
                }
            }

            foreach (var record in productRecords)
            {
                ProductEvidence? evidence = null;
                if (record.DesiredStatus != ProductStatuses.Draft)
                {
                    var objectKey = $"clean-v4/products/{record.Product.Id}/minh-chung.pdf";
                    await using (var stream = File.OpenRead(evidenceSource))
                        await storage.PutAsync(objectKey, stream, evidenceInfo.Length, "application/pdf", ct);
                    newObjectKeys.Add(objectKey);
                    evidence = new ProductEvidence(record.Product.Id, record.OwnerAccountId,
                        $"Minh-chung-{record.TypeCode}-{record.Year.Code}.pdf", objectKey, evidenceInfo.Length,
                        "Minh chứng mẫu có thể tải và kiểm tra trong luồng xét duyệt.");
                    db.ProductEvidences.Add(evidence);
                    await db.SaveChangesAsync(ct);
                }
                AddReviewHistory(db, record, evidence);
                if (record.DesiredStatus != ProductStatuses.Draft)
                    db.ProductSnapshots.Add(new ProductSnapshot(record.Product, record.OwnerAccountId, 1));
                await db.SaveChangesAsync(ct);
            }

            await AddCoAuthors(db, productRecords, activeLecturers, departments, ct);
            await CreateTargets(db, currentYear, activeLecturers, demoLecturerId, types, departments, deans, adminId, productRecords, ct);
            await CreateEvaluations(db, years, activeLecturers, demoLecturerId, departments, deans, adminId, productRecords, ct);
            await CreateNotifications(db, currentYear, activeLecturers, demoLecturerId, productRecords, ct);
            await db.Database.ExecuteSqlRawAsync(
                $"INSERT INTO nckh.ung_dung_migration(ma) VALUES ('{Version}') ON CONFLICT (ma) DO NOTHING", ct);
            await transaction.CommitAsync(ct);

            foreach (var key in oldObjectKeys.Distinct())
                try { await storage.DeleteAsync(key, ct); } catch { /* dữ liệu DB đã nhất quán; object cũ có thể dọn sau */ }
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            foreach (var key in newObjectKeys)
                try { await storage.DeleteAsync(key, CancellationToken.None); } catch { }
            throw;
        }
    }

    private static string CurrentStatus(int variant, string typeCode) => variant switch
    {
        0 => ProductStatuses.Approved,
        1 when typeCode == "BAI_BAO" => ProductStatuses.DepartmentHeadApproved,
        1 => ProductStatuses.Submitted,
        2 => ProductStatuses.Returned,
        _ => ProductStatuses.Draft
    };

    private static void ApplyStatus(Product product, string status)
    {
        if (status == ProductStatuses.Draft) return;
        product.Submit();
        if (status == ProductStatuses.Submitted) return;
        if (status == ProductStatuses.Returned) { product.Return(); return; }
        product.Approve(ProductStatuses.DepartmentHeadApproved);
        if (status == ProductStatuses.DepartmentHeadApproved) return;
        product.Approve(ProductStatuses.Approved);
    }

    private static void AddReviewHistory(ApplicationDbContext db,
        (Product Product, ResearchPlanItem Item, string TypeCode, AcademicYear Year, Lecturer Lecturer,
            string DesiredStatus, long OwnerAccountId, long ReviewerHeadId, long ReviewerDeanId) r,
        ProductEvidence? evidence)
    {
        if (r.DesiredStatus == ProductStatuses.Draft) return;
        db.ReviewHistories.Add(new ReviewHistory(r.Product.Id, r.OwnerAccountId,
            ProductStatuses.Draft, ProductStatuses.Submitted, "Giảng viên gửi sản phẩm và minh chứng."));
        if (r.DesiredStatus == ProductStatuses.Returned)
        {
            db.ReviewHistories.Add(new ReviewHistory(r.Product.Id, r.ReviewerHeadId,
                ProductStatuses.Submitted, ProductStatuses.Returned, "Bổ sung căn cứ và minh chứng chuyên môn."));
            return;
        }
        if (r.DesiredStatus is ProductStatuses.DepartmentHeadApproved or ProductStatuses.Approved)
            db.ReviewHistories.Add(new ReviewHistory(r.Product.Id, r.ReviewerHeadId,
                ProductStatuses.Submitted, ProductStatuses.DepartmentHeadApproved, "Bộ môn xác nhận nội dung và tác giả."));
        if (r.DesiredStatus == ProductStatuses.Approved)
            db.ReviewHistories.Add(new ReviewHistory(r.Product.Id, r.ReviewerDeanId,
                ProductStatuses.DepartmentHeadApproved, ProductStatuses.Approved, "Trưởng khoa công nhận sản phẩm và phân loại quy đổi."));
        if (r.TypeCode != "BAI_BAO" || evidence is null) return;
        var first = new ArticleProgressRequest(r.Product.Id, r.OwnerAccountId, "DANG_VIET", "DANG_PHAN_BIEN",
            evidence.Id, Roles.DepartmentHead, null, r.Product.PublicationInfo, "Đề nghị xác nhận đã gửi phản biện.");
        first.Approve(r.ReviewerHeadId, "Minh chứng gửi phản biện hợp lệ.");
        db.ArticleProgressRequests.Add(first);
        if (r.DesiredStatus != ProductStatuses.Approved) return;
        var second = new ArticleProgressRequest(r.Product.Id, r.OwnerAccountId, "DANG_PHAN_BIEN", "DA_NHAN_XET",
            evidence.Id, Roles.FacultyDean, null, r.Product.PublicationInfo, "Đề nghị xác nhận đã nhận phản hồi chuyên môn.");
        second.Approve(r.ReviewerDeanId, "Phản hồi chuyên môn hợp lệ.");
        var third = new ArticleProgressRequest(r.Product.Id, r.OwnerAccountId, "DA_NHAN_XET", "DA_XUAT_BAN",
            evidence.Id, Roles.FacultyDean, r.Product.PublishedDate, r.Product.PublicationInfo, "Đề nghị xác nhận công bố chính thức.");
        third.Approve(r.ReviewerDeanId, "Ngày và nơi công bố hợp lệ.");
        db.ArticleProgressRequests.AddRange(second, third);
    }

    private static async Task AddCoAuthors(ApplicationDbContext db,
        List<(Product Product, ResearchPlanItem Item, string TypeCode, AcademicYear Year, Lecturer Lecturer,
            string DesiredStatus, long OwnerAccountId, long ReviewerHeadId, long ReviewerDeanId)> records,
        List<Lecturer> lecturers, Dictionary<long, Department> departments, CancellationToken ct)
    {
        foreach (var (record, index) in records.Select((value, index) => (value, index)).Where(x => x.index % 7 == 0))
        {
            var facultyId = departments[record.Lecturer.DepartmentId].FacultyId;
            var coAuthor = lecturers.FirstOrDefault(x => x.Id != record.Lecturer.Id && departments[x.DepartmentId].FacultyId == facultyId);
            if (coAuthor is not null) db.ProductCoAuthors.Add(new ProductCoAuthor(record.Product.Id, coAuthor.Id, 2));
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task CreateTargets(ApplicationDbContext db, AcademicYear year, List<Lecturer> lecturers,
        long demoLecturerId, Dictionary<string, ProductType> types, Dictionary<long, Department> departments,
        Dictionary<long, long> deans, long adminId,
        List<(Product Product, ResearchPlanItem Item, string TypeCode, AcademicYear Year, Lecturer Lecturer,
            string DesiredStatus, long OwnerAccountId, long ReviewerHeadId, long ReviewerDeanId)> records,
        CancellationToken ct)
    {
        foreach (var facultyId in lecturers.Where(x => x.Id != demoLecturerId).Select(x => departments[x.DepartmentId].FacultyId).Distinct())
        foreach (var typeCode in new[] { "BAI_BAO", "DE_TAI", "SACH", "HOI_NGHI_HOI_THAO" })
        {
            var facultyLecturers = lecturers.Where(x => x.Id != demoLecturerId && departments[x.DepartmentId].FacultyId == facultyId).ToList();
            var actor = deans.GetValueOrDefault(facultyId, adminId);
            var target = new ResearchTarget(facultyId, year.Id, facultyLecturers.Count, year.EndDate.AddDays(-30), productTypeId: types[typeCode].Id);
            target.MarkAssigned(actor); target.Accept(actor, DateTimeOffset.UtcNow); target.Publish(DateTimeOffset.UtcNow); target.Lock(DateTimeOffset.UtcNow);
            db.ResearchTargets.Add(target); await db.SaveChangesAsync(ct);
            foreach (var departmentId in facultyLecturers.Select(x => x.DepartmentId).Distinct())
            {
                var members = facultyLecturers.Where(x => x.DepartmentId == departmentId).ToList();
                var allocation = new ResearchTargetAllocation(target.Id, facultyId, departmentId, members.Count, year.StartDate.AddDays(30));
                db.ResearchTargetAllocations.Add(allocation); await db.SaveChangesAsync(ct);
                foreach (var lecturer in members)
                    db.LecturerTargetAllocations.Add(new LecturerTargetAllocation(allocation.Id, lecturer.Id, 1, year.StartDate.AddDays(31)));
            }
            var products = records.Where(x => x.Year.Id == year.Id && x.TypeCode == typeCode &&
                departments[x.Lecturer.DepartmentId].FacultyId == facultyId).Select(x => x.Product);
            foreach (var product in products) product.LinkTarget(target.Id, product.JournalConferenceId);
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task CreateEvaluations(ApplicationDbContext db, List<AcademicYear> years,
        List<Lecturer> lecturers, long demoLecturerId, Dictionary<long, Department> departments,
        Dictionary<long, long> deans, long adminId,
        List<(Product Product, ResearchPlanItem Item, string TypeCode, AcademicYear Year, Lecturer Lecturer,
            string DesiredStatus, long OwnerAccountId, long ReviewerHeadId, long ReviewerDeanId)> records,
        CancellationToken ct)
    {
        var rules = await db.ConversionRules.AsNoTracking().Where(x => x.Unit == "GIO" && x.Code.StartsWith("QD2023_") && x.ClassificationId != null)
            .OrderByDescending(x => x.Priority).ThenByDescending(x => x.Version).ToListAsync(ct);
        foreach (var year in years)
        foreach (var lecturer in lecturers.Where(x => x.Id != demoLecturerId))
        {
            var approved = records.Where(x => x.Year.Id == year.Id && x.Lecturer.Id == lecturer.Id && x.DesiredStatus == ProductStatuses.Approved).ToList();
            if (approved.Count == 0) continue;
            var facultyId = departments[lecturer.DepartmentId].FacultyId;
            var evaluator = deans.GetValueOrDefault(facultyId, adminId);
            var total = approved.Sum(x => rules.First(r => r.ClassificationId == x.Product.ClassificationId).ConversionValue * x.Product.RecognitionQuantity);
            var evaluation = new Evaluation(lecturer.Id, year.Id, 1, evaluator, lecturer.Code, lecturer.FullName,
                lecturer.DepartmentId, lecturer.AcademicRank, lecturer.Degree, "GIO");
            evaluation.Finalize(total >= 586 ? "Hoàn thành" : "Chưa hoàn thành", $"Tổng {total:0.####}/586 giờ từ sản phẩm đã công nhận và phân loại QD2023.");
            db.Evaluations.Add(evaluation); await db.SaveChangesAsync(ct);
            foreach (var record in approved)
            {
                var rule = rules.First(r => r.ClassificationId == record.Product.ClassificationId);
                db.EvaluationConversionDetails.Add(new EvaluationConversionDetail(evaluation.Id, lecturer.Id, year.Id,
                    record.Product.Id, record.Item.ProductTypeId, rule.Id, rule.ConversionValue * record.Product.RecognitionQuantity,
                    1m, "GIO", $"{rule.Code} v{rule.Version}; tác giả chính; dữ liệu seed v4."));
            }
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task CreateNotifications(ApplicationDbContext db, AcademicYear year,
        List<Lecturer> lecturers, long demoLecturerId,
        List<(Product Product, ResearchPlanItem Item, string TypeCode, AcademicYear Year, Lecturer Lecturer,
            string DesiredStatus, long OwnerAccountId, long ReviewerHeadId, long ReviewerDeanId)> records,
        CancellationToken ct)
    {
        foreach (var lecturer in lecturers.Where(x => x.Id != demoLecturerId))
        {
            var notification = new Notification(lecturer.AccountId, "HE_THONG",
                $"Chỉ tiêu NCKH năm học {year.Code} đã được phân công theo bốn nhóm sản phẩm.",
                $"CLEAN_V4_TARGETS:{lecturer.Id}", actionPath: "my-targets");
            notification.MarkAsSent(); db.Notifications.Add(notification);
        }
        foreach (var record in records.Where(x => x.DesiredStatus == ProductStatuses.Returned && x.Year.Id == year.Id))
        {
            var notification = new Notification(record.OwnerAccountId, "PHAN_HOI",
                $"Sản phẩm '{record.Product.Title}' cần bổ sung minh chứng.",
                $"CLEAN_V4_RETURNED:{record.Product.Id}", scientificProductId: record.Product.Id,
                actionPath: $"products:{record.Product.Id}");
            notification.MarkAsSent(); db.Notifications.Add(notification);
        }
        await db.SaveChangesAsync(ct);
    }

    private static DateOnly? RecognitionDate(string typeCode, string yearCode) => (yearCode, typeCode) switch
    {
        ("2025-2026", "BAI_BAO") => new(2026, 3, 15),
        ("2025-2026", "DE_TAI") => new(2026, 5, 20),
        ("2025-2026", "SACH") => new(2026, 6, 10),
        ("2025-2026", _) => new(2025, 12, 5),
        ("2026-2027", "BAI_BAO") => new(2026, 9, 20),
        ("2026-2027", "DE_TAI") => new(2026, 10, 10),
        ("2026-2027", "SACH") => new(2026, 10, 25),
        ("2026-2027", _) => new(2026, 11, 5),
        _ => null
    };

    private static string ItemName(string code, string lecturer, string year) => code switch
    {
        "BAI_BAO" => $"Bài báo phân tích dữ liệu giáo dục - {lecturer} ({year})",
        "DE_TAI" => $"Đề tài hỗ trợ quản lý nghiên cứu khoa học - {lecturer} ({year})",
        "SACH" => $"Giáo trình chuyên ngành công nghệ thông tin - {lecturer} ({year})",
        _ => $"Báo cáo hội thảo về chuyển đổi số giáo dục - {lecturer} ({year})"
    };
    private static string ProductTitle(string code, string lecturer, string year) => ItemName(code, lecturer, year);
    private static string Publication(string code, string? articleVenue, string? conferenceVenue) => code switch
    {
        "BAI_BAO" => articleVenue ?? "Tạp chí khoa học có phản biện",
        "HOI_NGHI_HOI_THAO" => conferenceVenue ?? "Hội nghị khoa học quốc gia có ISBN",
        "SACH" => "Nhà xuất bản Giáo dục Việt Nam",
        _ => "Trường Đại học Công Thương TP.HCM"
    };
}
