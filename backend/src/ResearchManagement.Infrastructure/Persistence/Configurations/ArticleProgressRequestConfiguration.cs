using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ArticleProgressRequestConfiguration : IEntityTypeConfiguration<ArticleProgressRequest>
{
    public void Configure(EntityTypeBuilder<ArticleProgressRequest> b)
    {
        b.ToTable("de_nghi_tien_do_bai_bao", "nckh");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ProductId).HasColumnName("san_pham_id");
        b.Property(x => x.RequestedByAccountId).HasColumnName("nguoi_de_nghi_id");
        b.Property(x => x.FromStatus).HasColumnName("tu_trang_thai").HasMaxLength(30);
        b.Property(x => x.ToStatus).HasColumnName("den_trang_thai").HasMaxLength(30);
        b.Property(x => x.EvidenceId).HasColumnName("minh_chung_id");
        b.Property(x => x.RequiredRole).HasColumnName("vai_tro_duyet").HasMaxLength(30);
        b.Property(x => x.PublishedDate).HasColumnName("ngay_cong_bo").HasColumnType("date");
        b.Property(x => x.PublicationInfo).HasColumnName("thong_tin_xuat_ban").HasMaxLength(500);
        b.Property(x => x.Note).HasColumnName("ghi_chu_de_nghi").HasMaxLength(1000);
        b.Property(x => x.Status).HasColumnName("trang_thai").HasMaxLength(20);
        b.Property(x => x.ReviewedByAccountId).HasColumnName("nguoi_xet_duyet_id");
        b.Property(x => x.ReviewComment).HasColumnName("ghi_chu_xet_duyet").HasMaxLength(1000);
        b.Property(x => x.RequestedAt).HasColumnName("de_nghi_luc").HasColumnType("timestamptz");
        b.Property(x => x.ReviewedAt).HasColumnName("xet_duyet_luc").HasColumnType("timestamptz");
        b.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<ProductEvidence>().WithMany().HasForeignKey(x => x.EvidenceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Account>().WithMany().HasForeignKey(x => x.ReviewedByAccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
