using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class EmulationClassificationRuleConfiguration : IEntityTypeConfiguration<EmulationClassificationRule>
{
    public void Configure(EntityTypeBuilder<EmulationClassificationRule> b)
    {
        b.ToTable("quy_che_xep_loai_thi_dua", "nckh");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.AcademicYearId).HasColumnName("nam_hoc_id");
        b.Property(x => x.Name).HasColumnName("ten_xep_loai").HasMaxLength(100).IsRequired();
        b.Property(x => x.MinimumPercent).HasColumnName("ty_le_toi_thieu").HasPrecision(7, 2).IsRequired();
        b.Property(x => x.SortOrder).HasColumnName("thu_tu").IsRequired();
        b.HasIndex(x => new { x.AcademicYearId, x.Name }).IsUnique();
        b.HasIndex(x => new { x.AcademicYearId, x.MinimumPercent }).IsUnique();
        b.HasOne<AcademicYear>().WithMany().HasForeignKey(x => x.AcademicYearId).OnDelete(DeleteBehavior.Cascade);
    }
}
