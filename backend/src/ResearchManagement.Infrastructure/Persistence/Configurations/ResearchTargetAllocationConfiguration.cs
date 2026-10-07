using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ResearchTargetAllocationConfiguration
    : IEntityTypeConfiguration<ResearchTargetAllocation>
{
    public void Configure(EntityTypeBuilder<ResearchTargetAllocation> builder)
    {
        builder.ToTable("phan_bo_chi_tieu", "nckh");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.ResearchTargetId)
            .HasColumnName("chi_tieu_id")
            .IsRequired();

        builder.Property(x => x.FacultyId)
            .HasColumnName("khoa_id")
            .IsRequired();

        builder.Property(x => x.DepartmentId)
            .HasColumnName("bo_mon_id")
            .IsRequired();

        builder.Property(x => x.AllocatedArticleCount)
            .HasColumnName("so_bai_phan_bo")
            .IsRequired();

        builder.Property(x => x.AllocationDate)
            .HasColumnName("ngay_phan_bo")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.ResearchTargetId,
            x.DepartmentId
        })
        .IsUnique();
    }
}