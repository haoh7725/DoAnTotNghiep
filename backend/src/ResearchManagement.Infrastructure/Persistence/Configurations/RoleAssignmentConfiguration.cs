using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class RoleAssignmentConfiguration : IEntityTypeConfiguration<RoleAssignment>
{
    public void Configure(EntityTypeBuilder<RoleAssignment> builder)
    {
        builder.ToTable("phan_quyen", "nckh");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.AccountId).HasColumnName("tai_khoan_id");
        builder.Property(r => r.Role).HasColumnName("vai_tro").HasMaxLength(30).IsRequired();
        builder.Property(r => r.Scope).HasColumnName("pham_vi").HasMaxLength(20).IsRequired();
        builder.Property(r => r.FacultyId).HasColumnName("khoa_id");
        builder.Property(r => r.DepartmentId).HasColumnName("bo_mon_id");
    }
}
