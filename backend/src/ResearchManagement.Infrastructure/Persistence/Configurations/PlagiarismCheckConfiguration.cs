using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Constants;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class PlagiarismCheckConfiguration : IEntityTypeConfiguration<PlagiarismCheck>
{
    public void Configure(EntityTypeBuilder<PlagiarismCheck> builder)
    {
        builder.ToTable("kiem_tra_tuong_dong");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ProductId).HasColumnName("san_pham_id");
        builder.Property(x => x.EvidenceId).HasColumnName("minh_chung_id");
        builder.Property(x => x.Provider).HasColumnName("nha_cung_cap").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ExternalScanId).HasColumnName("external_scan_id").HasMaxLength(100);
        builder.Property(x => x.Status).HasColumnName("trang_thai").HasMaxLength(30).IsRequired();
        builder.Property(x => x.SimilarityScore).HasColumnName("ty_le_tuong_dong").HasPrecision(5, 2);
        builder.Property(x => x.MatchedSourceCount).HasColumnName("so_nguon_trung");
        builder.Property(x => x.TotalWords).HasColumnName("tong_so_tu");
        builder.Property(x => x.CreditsUsed).HasColumnName("so_credit");
        builder.Property(x => x.FileHash).HasColumnName("file_hash").HasMaxLength(64).IsRequired();
        builder.Property(x => x.ReportUrl).HasColumnName("report_url");
        builder.Property(x => x.ReportObjectKey).HasColumnName("report_object_key").HasMaxLength(500);
        builder.Property(x => x.ErrorMessage).HasColumnName("loi");
        builder.Property(x => x.RequestedByAccountId).HasColumnName("nguoi_yeu_cau_id");
        builder.Property(x => x.ThirdPartyConsent).HasColumnName("da_dong_y_gui_ben_thu_ba");
        builder.Property(x => x.CreatedAt).HasColumnName("tao_luc");
        builder.Property(x => x.StartedAt).HasColumnName("bat_dau_luc");
        builder.Property(x => x.CompletedAt).HasColumnName("hoan_thanh_luc");
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductEvidence>().WithMany().HasForeignKey(x => x.EvidenceId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Account>().WithMany().HasForeignKey(x => x.RequestedByAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.ProductId, x.CreatedAt });
        builder.HasIndex(x => new { x.EvidenceId, x.CreatedAt });
        builder.HasIndex(x => new { x.Provider, x.ExternalScanId }).IsUnique()
            .HasFilter("external_scan_id IS NOT NULL");
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_kiem_tra_tuong_dong_trang_thai",
                $"trang_thai IN ('{PlagiarismCheckStatuses.Pending}','{PlagiarismCheckStatuses.Sending}','{PlagiarismCheckStatuses.Processing}','{PlagiarismCheckStatuses.Completed}','{PlagiarismCheckStatuses.Failed}','{PlagiarismCheckStatuses.Cancelled}')");
            t.HasCheckConstraint("ck_kiem_tra_tuong_dong_ty_le",
                "ty_le_tuong_dong IS NULL OR ty_le_tuong_dong BETWEEN 0 AND 100");
        });
    }
}
