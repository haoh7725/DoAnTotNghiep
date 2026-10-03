using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class ResearchPlanConfiguration : IEntityTypeConfiguration<ResearchPlan>
{
    public void Configure(EntityTypeBuilder<ResearchPlan> builder)
    {
        builder.ToTable("ke_hoach_nckh", "nckh");

        builder.HasKey(plan => plan.Id);

        builder.Property(plan => plan.Id)
            .HasColumnName("id");

        builder.Property(plan => plan.LecturerId)
            .HasColumnName("giang_vien_id")
            .IsRequired();

        builder.Property(plan => plan.AcademicYearId)
            .HasColumnName("nam_hoc_id")
            .IsRequired();

        builder.Property(plan => plan.CommittedProductCount)
            .HasColumnName("so_bai_cam_ket")
            .IsRequired();

        builder.Property(plan => plan.PlanDate)
            .HasColumnName("ngay_lap")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(plan => plan.Status)
            .HasColumnName("trang_thai")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(plan => plan.Deadline)
            .HasColumnName("han_hoan_thanh")
            .HasColumnType("date");

        builder.HasIndex(plan => new
        {
            plan.LecturerId,
            plan.AcademicYearId
        })
        .IsUnique();
    }
}