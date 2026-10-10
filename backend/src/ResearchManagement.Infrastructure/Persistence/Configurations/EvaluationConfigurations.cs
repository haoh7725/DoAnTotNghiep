using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class EvaluationConfiguration : IEntityTypeConfiguration<Evaluation>
{
    public void Configure(EntityTypeBuilder<Evaluation> b)
    {
        b.ToTable("ket_qua_danh_gia", "nckh"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.LecturerId).HasColumnName("giang_vien_id");
        b.Property(x => x.AcademicYearId).HasColumnName("nam_hoc_id");
        b.Property(x => x.Attempt).HasColumnName("lan_danh_gia");
        b.Property(x => x.EvaluatorAccountId).HasColumnName("nguoi_danh_gia_id");
        b.Property(x => x.Status).HasColumnName("trang_thai").HasMaxLength(10);
        b.Property(x => x.Classification).HasColumnName("xep_loai").HasMaxLength(100);
        b.Property(x => x.ClassificationBasis).HasColumnName("can_cu_xep_loai");
        b.Property(x => x.LecturerCodeSnapshot).HasColumnName("ma_giang_vien_anh_chup").HasMaxLength(30);
        b.Property(x => x.LecturerNameSnapshot).HasColumnName("ho_ten_giang_vien_anh_chup").HasMaxLength(200);
        b.Property(x => x.DepartmentIdSnapshot).HasColumnName("bo_mon_id_anh_chup");
        b.Property(x => x.AcademicRankSnapshot).HasColumnName("hoc_ham_anh_chup").HasMaxLength(100);
        b.Property(x => x.DegreeSnapshot).HasColumnName("hoc_vi_anh_chup").HasMaxLength(100);
        b.Property(x => x.EvaluatedAt).HasColumnName("ngay_danh_gia").HasColumnType("timestamptz");
        b.HasIndex(x => new { x.LecturerId, x.AcademicYearId, x.Attempt }).IsUnique();
    }
}

public sealed class EvaluationConversionDetailConfiguration : IEntityTypeConfiguration<EvaluationConversionDetail>
{
    public void Configure(EntityTypeBuilder<EvaluationConversionDetail> b)
    {
        b.ToTable("chi_tiet_quy_doi_ung_dung", "nckh"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.EvaluationId).HasColumnName("ket_qua_id");
        b.Property(x => x.LecturerId).HasColumnName("giang_vien_id");
        b.Property(x => x.AcademicYearId).HasColumnName("nam_hoc_id");
        b.Property(x => x.ProductId).HasColumnName("san_pham_id");
        b.Property(x => x.ProductTypeId).HasColumnName("loai_san_pham_id");
        b.Property(x => x.ConversionRuleId).HasColumnName("quy_dinh_id");
        b.Property(x => x.BaseValue).HasColumnName("gia_tri_goc").HasPrecision(14, 4);
        b.Property(x => x.AuthorCoefficient).HasColumnName("he_so_tac_gia").HasPrecision(12, 6);
        b.Property(x => x.ConvertedValue).HasColumnName("gia_tri_quy_doi").HasPrecision(14, 4);
        b.Property(x => x.Unit).HasColumnName("don_vi").HasMaxLength(10);
        b.Property(x => x.CalculationBasis).HasColumnName("can_cu_tinh");
        b.HasIndex(x => new { x.EvaluationId, x.ProductId, x.Unit }).IsUnique();
        b.HasOne<Evaluation>().WithMany().HasForeignKey(x => x.EvaluationId).OnDelete(DeleteBehavior.Cascade);
    }
}
