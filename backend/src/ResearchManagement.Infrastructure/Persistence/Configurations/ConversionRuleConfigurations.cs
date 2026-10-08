using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ConversionRuleConfiguration : IEntityTypeConfiguration<ConversionRule>
{
    public void Configure(EntityTypeBuilder<ConversionRule> b)
    {
        b.ToTable("quy_dinh_quy_doi", "nckh"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.ProductTypeId).HasColumnName("loai_san_pham_id");
        b.Property(x => x.Code).HasColumnName("ma_quy_dinh").HasMaxLength(50); b.Property(x => x.Version).HasColumnName("phien_ban");
        b.Property(x => x.Name).HasColumnName("ten_quy_dinh").HasMaxLength(300); b.Property(x => x.ConditionDescription).HasColumnName("mo_ta_dieu_kien");
        b.Property(x => x.ConversionValue).HasColumnName("muc_quy_doi").HasPrecision(14, 4); b.Property(x => x.Unit).HasColumnName("don_vi").HasMaxLength(10);
        b.Property(x => x.AuthorRuleDescription).HasColumnName("mo_ta_quy_tac_tac_gia"); b.Property(x => x.EffectiveFrom).HasColumnName("hieu_luc_tu").HasColumnType("date");
        b.Property(x => x.EffectiveTo).HasColumnName("hieu_luc_den").HasColumnType("date"); b.Property(x => x.LegalBasis).HasColumnName("van_ban_can_cu").HasMaxLength(512);
        b.HasOne<ProductType>().WithMany().HasForeignKey(x => x.ProductTypeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ConversionCriterionConfiguration : IEntityTypeConfiguration<ConversionCriterion>
{
    public void Configure(EntityTypeBuilder<ConversionCriterion> b)
    {
        b.ToTable("tieu_chi_quy_doi", "nckh"); b.HasKey(x => x.Id); b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ConversionRuleId).HasColumnName("quy_dinh_id"); b.Property(x => x.Field).HasColumnName("truong_du_lieu").HasMaxLength(40);
        b.Property(x => x.Operator).HasColumnName("toan_tu").HasMaxLength(12); b.Property(x => x.StringValue).HasColumnName("gia_tri_chuoi").HasMaxLength(300);
        b.Property(x => x.NumericValue).HasColumnName("gia_tri_so").HasPrecision(14, 4);
        b.HasOne<ConversionRule>().WithMany().HasForeignKey(x => x.ConversionRuleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AuthorConversionCoefficientConfiguration : IEntityTypeConfiguration<AuthorConversionCoefficient>
{
    public void Configure(EntityTypeBuilder<AuthorConversionCoefficient> b)
    {
        b.ToTable("he_so_tac_gia_quy_doi", "nckh"); b.HasKey(x => new { x.ConversionRuleId, x.AuthorRole });
        b.Property(x => x.ConversionRuleId).HasColumnName("quy_dinh_id"); b.Property(x => x.AuthorRole).HasColumnName("vai_tro_tac_gia").HasMaxLength(100);
        b.Property(x => x.Coefficient).HasColumnName("he_so").HasPrecision(12, 6);
        b.HasOne<ConversionRule>().WithMany().HasForeignKey(x => x.ConversionRuleId).OnDelete(DeleteBehavior.Cascade);
    }
}
