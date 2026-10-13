using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductSnapshotConfiguration : IEntityTypeConfiguration<ProductSnapshot>
{
    public void Configure(EntityTypeBuilder<ProductSnapshot> builder)
    {
        builder.ToTable("san_pham_phien_ban", "nckh");

        // Composite PK: (ProductId, Version)
        builder.HasKey(x => new { x.ProductId, x.Version });

        builder.Property(x => x.ProductId).HasColumnName("san_pham_id").IsRequired();
        builder.Property(x => x.Version).HasColumnName("phien_ban").IsRequired();
        builder.Property(x => x.SnapshotByAccountId).HasColumnName("nguoi_cap_nhat_id").IsRequired();
        builder.Property(x => x.SnapshotAt).HasColumnName("ghi_luc").HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.Title).HasColumnName("tieu_de").HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasColumnName("mo_ta");
        builder.Property(x => x.PublicationInfo).HasColumnName("thong_tin_xuat_ban").HasMaxLength(500);
        builder.Property(x => x.PublishedDate).HasColumnName("ngay_cong_bo").HasColumnType("date");
        builder.Property(x => x.ArticleStatus).HasColumnName("trang_thai_bai_bao").HasMaxLength(30);
        builder.Property(x => x.JournalIndex).HasColumnName("chi_so_tap_chi").HasMaxLength(100);
        builder.Property(x => x.JournalClassification).HasColumnName("phan_loai_tap_chi").HasMaxLength(100);
        builder.Property(x => x.ProjectLevel).HasColumnName("cap_de_tai").HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("trang_thai").HasMaxLength(30).IsRequired();

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
