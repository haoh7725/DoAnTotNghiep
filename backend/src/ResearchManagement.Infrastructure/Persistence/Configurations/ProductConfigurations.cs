using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("san_pham_khoa_hoc", "nckh");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProductTypeId).HasColumnName("loai_san_pham_id").IsRequired();
        builder.Property(x => x.AcademicYearId).HasColumnName("nam_hoc_ghi_nhan_id").IsRequired();
        builder.Property(x => x.CreatedByAccountId).HasColumnName("nguoi_tao_id").IsRequired();
        builder.Property(x => x.Code).HasColumnName("ma_san_pham").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Title).HasColumnName("ten_san_pham").HasMaxLength(500).IsRequired();
        builder.Property(x => x.PublicationYear).HasColumnName("nam_cong_bo");
        builder.Property(x => x.PublicationInfo).HasColumnName("thong_tin_cong_bo");
        builder.Property(x => x.Doi).HasColumnName("doi").HasMaxLength(255);
        builder.Property(x => x.Isbn).HasColumnName("isbn").HasMaxLength(30);
        builder.Property(x => x.Issn).HasColumnName("issn").HasMaxLength(20);
        builder.Property(x => x.JournalName).HasColumnName("ten_tap_chi_hoi_nghi").HasMaxLength(300);
        builder.Property(x => x.JournalIndex).HasColumnName("chi_so_tap_chi").HasMaxLength(100);
        builder.Property(x => x.JournalCategory).HasColumnName("phan_loai_tap_chi").HasMaxLength(100);
        builder.Property(x => x.WorkScore).HasColumnName("diem_cong_trinh").HasPrecision(10, 4);
        builder.Property(x => x.ResearchField).HasColumnName("linh_vuc_nghien_cuu").HasMaxLength(300);
        builder.Property(x => x.Publisher).HasColumnName("nha_xuat_ban").HasMaxLength(300);
        builder.Property(x => x.ProjectLevel).HasColumnName("cap_de_tai").HasMaxLength(100);
        builder.Property(x => x.HostUnit).HasColumnName("don_vi_chu_tri").HasMaxLength(300);
        builder.Property(x => x.ProjectObjective).HasColumnName("muc_tieu_de_tai");
        builder.Property(x => x.ProjectContent).HasColumnName("noi_dung_de_tai");
        builder.Property(x => x.ExpectedResult).HasColumnName("ket_qua_du_kien");
        builder.Property(x => x.CertificateNumber).HasColumnName("so_chung_nhan").HasMaxLength(100);
        builder.Property(x => x.IssuingAuthority).HasColumnName("co_quan_cap").HasMaxLength(300);
        builder.Property(x => x.StartDate).HasColumnName("ngay_bat_dau").HasColumnType("date");
        builder.Property(x => x.EndDate).HasColumnName("ngay_ket_thuc").HasColumnType("date");
        builder.Property(x => x.SubmittedDate).HasColumnName("ngay_nop").HasColumnType("date");
        builder.Property(x => x.PublishedDate).HasColumnName("ngay_xuat_ban").HasColumnType("date");
        builder.Property(x => x.ArticleStatus).HasColumnName("trang_thai_bai_bao").HasMaxLength(30);
        builder.Property(x => x.ReviewStatus).HasColumnName("trang_thai_duyet").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Version).HasColumnName("phien_ban").IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasOne<ProductType>().WithMany().HasForeignKey(x => x.ProductTypeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.CreatedByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductAuthorConfiguration : IEntityTypeConfiguration<ProductAuthor>
{
    public void Configure(EntityTypeBuilder<ProductAuthor> builder)
    {
        builder.ToTable("tham_gia_san_pham", "nckh");
        builder.HasKey(x => new { x.ProductId, x.LecturerId });

        builder.Property(x => x.ProductId).HasColumnName("san_pham_id");
        builder.Property(x => x.LecturerId).HasColumnName("giang_vien_id");
        builder.Property(x => x.DepartmentId).HasColumnName("bo_mon_id_ghi_nhan").IsRequired();
        builder.Property(x => x.Role).HasColumnName("vai_tro_tac_gia").HasMaxLength(100).IsRequired();
        builder.Property(x => x.Order).HasColumnName("thu_tu_tac_gia").IsRequired();

        // UNIQUE (san_pham_id, thu_tu_tac_gia) trong schema SQL.
        builder.HasIndex(x => new { x.ProductId, x.Order }).IsUnique();

        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Lecturer>().WithMany().HasForeignKey(x => x.LecturerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductEvidenceConfiguration : IEntityTypeConfiguration<ProductEvidence>
{
    public void Configure(EntityTypeBuilder<ProductEvidence> builder)
    {
        builder.ToTable("minh_chung", "nckh");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProductId).HasColumnName("san_pham_id").IsRequired();
        builder.Property(x => x.UploadedByAccountId).HasColumnName("nguoi_tai_id").IsRequired();
        builder.Property(x => x.FileName).HasColumnName("ten_tai_lieu").HasMaxLength(255).IsRequired();
        builder.Property(x => x.StorageKey).HasColumnName("khoa_luu_tru").HasMaxLength(512).IsRequired();
        builder.Property(x => x.MimeType).HasColumnName("mime_type").HasMaxLength(100).IsRequired();
        builder.Property(x => x.FileSizeBytes).HasColumnName("kich_thuoc").IsRequired();
        builder.Property(x => x.Sha256).HasColumnName("sha256").HasMaxLength(64);
        builder.Property(x => x.UploadedAt).HasColumnName("tai_luc").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.UploadedByAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ReviewHistoryConfiguration : IEntityTypeConfiguration<ReviewHistory>
{
    public void Configure(EntityTypeBuilder<ReviewHistory> builder)
    {
        builder.ToTable("lich_su_xet_duyet", "nckh");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProductId).HasColumnName("san_pham_id").IsRequired();
        builder.Property(x => x.ActorAccountId).HasColumnName("tai_khoan_id").IsRequired();
        builder.Property(x => x.FromStatus).HasColumnName("tu_trang_thai").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ToStatus).HasColumnName("sang_trang_thai").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Comment).HasColumnName("ghi_chu").HasMaxLength(1000);
        builder.Property(x => x.OccurredAt).HasColumnName("thoi_diem").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
