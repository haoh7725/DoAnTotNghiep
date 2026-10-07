using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ResearchNormConfiguration
    : IEntityTypeConfiguration<ResearchNorm>
{
    public void Configure(EntityTypeBuilder<ResearchNorm> builder)
    {
        builder.ToTable("dinh_muc_nckh", "nckh");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.LecturerId)
            .HasColumnName("giang_vien_id")
            .IsRequired();

        builder.Property(x => x.AcademicYearId)
            .HasColumnName("nam_hoc_id")
            .IsRequired();

        builder.Property(x => x.RequiredHours)
            .HasColumnName("so_gio_dinh_muc")
            .HasPrecision(12, 4)
            .IsRequired();

        builder.Property(x => x.Basis)
            .HasColumnName("can_cu");

        builder.HasIndex(x => new
        {
            x.LecturerId,
            x.AcademicYearId
        })
        .IsUnique();
    }
}