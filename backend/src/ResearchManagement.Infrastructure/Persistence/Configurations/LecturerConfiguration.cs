using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class LecturerConfiguration : IEntityTypeConfiguration<Lecturer>
{
    public void Configure(EntityTypeBuilder<Lecturer> builder)
    {
        builder.ToTable("giang_vien", "nckh");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.DepartmentId)
            .HasColumnName("bo_mon_id");

        builder.Property(x => x.AccountId)
            .HasColumnName("tai_khoan_id");

        builder.Property(x => x.LecturerCode)
            .HasColumnName("ma_giang_vien")
            .HasMaxLength(30);

        builder.Property(x => x.FullName)
            .HasColumnName("ho_ten")
            .HasMaxLength(200);
    }
}