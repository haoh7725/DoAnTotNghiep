using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("san_pham", "nckh");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ResearchPlanItemId).HasColumnName("noi_dung_ke_hoach_id").IsRequired();
        builder.Property(x => x.SubmittedByLecturerId).HasColumnName("giang_vien_id").IsRequired();
        builder.Property(x => x.Title).HasColumnName("tieu_de").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasColumnName("mo_ta");
        builder.Property(x => x.PublicationInfo).HasColumnName("thong_tin_xuat_ban").HasMaxLength(500);
        builder.Property(x => x.PublishedDate).HasColumnName("ngay_cong_bo").HasColumnType("date");
        builder.Property(x => x.Status).HasColumnName("trang_thai").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ScoreEquivalent).HasColumnName("diem_quy_doi").HasPrecision(5, 2);
        builder.Property(x => x.CreatedAt).HasColumnName("tao_luc").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("cap_nhat_luc").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<ResearchPlanItem>()
            .WithMany()
            .HasForeignKey(x => x.ResearchPlanItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Lecturer>()
            .WithMany()
            .HasForeignKey(x => x.SubmittedByLecturerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ProductCoAuthorConfiguration : IEntityTypeConfiguration<ProductCoAuthor>
{
    public void Configure(EntityTypeBuilder<ProductCoAuthor> builder)
    {
        builder.ToTable("dong_tac_gia", "nckh");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProductId).HasColumnName("san_pham_id").IsRequired();
        builder.Property(x => x.LecturerId).HasColumnName("giang_vien_id").IsRequired();
        builder.Property(x => x.DisplayOrder).HasColumnName("thu_tu").IsRequired();

        // Mỗi giảng viên chỉ xuất hiện một lần trong một sản phẩm
        builder.HasIndex(x => new { x.ProductId, x.LecturerId }).IsUnique();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Lecturer>()
            .WithMany()
            .HasForeignKey(x => x.LecturerId)
            .OnDelete(DeleteBehavior.Restrict);
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
        builder.Property(x => x.UploadedByAccountId).HasColumnName("tai_khoan_id").IsRequired();
        builder.Property(x => x.OriginalFileName).HasColumnName("ten_file_goc").HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoredPath).HasColumnName("duong_dan_luu").HasMaxLength(500).IsRequired();
        builder.Property(x => x.FileSizeBytes).HasColumnName("kich_thuoc_bytes").IsRequired();
        builder.Property(x => x.Description).HasColumnName("mo_ta").HasMaxLength(300);
        builder.Property(x => x.UploadedAt).HasColumnName("upload_luc").HasColumnType("timestamptz").IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
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
