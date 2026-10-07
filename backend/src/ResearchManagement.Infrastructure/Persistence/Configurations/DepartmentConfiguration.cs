using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("bo_mon", "nckh");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id");

        builder.Property(x => x.FacultyId)
            .HasColumnName("khoa_id");

        builder.Property(x => x.Code)
            .HasColumnName("ma_bo_mon")
            .HasMaxLength(30);

        builder.Property(x => x.Name)
            .HasColumnName("ten_bo_mon")
            .HasMaxLength(200);
    }
}