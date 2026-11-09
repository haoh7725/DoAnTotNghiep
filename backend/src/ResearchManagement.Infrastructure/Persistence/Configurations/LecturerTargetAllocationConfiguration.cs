using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class LecturerTargetAllocationConfiguration : IEntityTypeConfiguration<LecturerTargetAllocation>
{
    public void Configure(EntityTypeBuilder<LecturerTargetAllocation> builder)
    {
        builder.ToTable("phan_cong_chi_tieu_giang_vien", "nckh");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DepartmentAllocationId).HasColumnName("phan_bo_bo_mon_id").IsRequired();
        builder.Property(x => x.LecturerId).HasColumnName("giang_vien_id").IsRequired();
        builder.Property(x => x.AllocatedArticleCount).HasColumnName("so_bai_phan_cong").IsRequired();
        builder.Property(x => x.AllocationDate).HasColumnName("ngay_phan_cong").IsRequired();
        builder.HasIndex(x => new { x.DepartmentAllocationId, x.LecturerId }).IsUnique();
        builder.HasOne<ResearchTargetAllocation>().WithMany().HasForeignKey(x => x.DepartmentAllocationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Lecturer>().WithMany().HasForeignKey(x => x.LecturerId).OnDelete(DeleteBehavior.Restrict);
    }
}
