using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ResearchTargetConfiguration
    : IEntityTypeConfiguration<ResearchTarget>
{
    public void Configure(EntityTypeBuilder<ResearchTarget> builder)
    {
        builder.ToTable("chi_tieu_nckh", "nckh");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.FacultyId)
            .HasColumnName("khoa_id")
            .IsRequired();

        builder.Property(x => x.AcademicYearId)
            .HasColumnName("nam_hoc_id")
            .IsRequired();

        builder.Property(x => x.AssignedArticleCount)
            .HasColumnName("so_bai_duoc_giao")
            .IsRequired();

        builder.Property(x => x.Deadline)
            .HasColumnName("han_hoan_thanh");

        builder.HasIndex(x => new
        {
            x.FacultyId,
            x.AcademicYearId
        })
        .IsUnique();
    }
}